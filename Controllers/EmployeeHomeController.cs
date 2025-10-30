using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using Leave_Management_system.Models;
using Leave_Management_system.viewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management_system.Controllers
{
    public class EmployeeHomeController : Controller
    {
        private readonly FirestoreDb _db;

        public EmployeeHomeController(FirestoreDb db)
        {
            _db = db;
        }

        // GET: EmployeeHome/Index
        public async Task<IActionResult> Index(string employeeId = null)
        {
            // Get EmployeeId from session first
            var employeeIdSession = HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeIdSession))
                return RedirectToAction("Login", "EmployeeAuth");

            var resolvedEmployeeId = !string.IsNullOrEmpty(employeeIdSession) ? employeeIdSession : employeeId;

            if (string.IsNullOrEmpty(resolvedEmployeeId))
                return Content("Employee not Identified. Provide EmployeeID");

            var vm = new EmployeeHomeViewModel();

            // Load employee profile
            var empSnap = await _db.Collection("Employees").Document(resolvedEmployeeId).GetSnapshotAsync();
            if (empSnap.Exists)
            {
                var map = empSnap.ToDictionary();
                vm.Profile = new EmployeeProfileViewModel
                {
                    EmployeeId = empSnap.Id,
                    FullName = map.ContainsKey("FullName") ? map["FullName"]?.ToString() : "No name",
                    Email = map.ContainsKey("Email") ? map["Email"]?.ToString() : "",
                    Phone = map.ContainsKey("Phone") ? map["Phone"]?.ToString() : "",
                    Department = map.ContainsKey("Department") ? map["Department"]?.ToString() : "",
                    PhotoUrl = map.ContainsKey("PhotoUrl") ? map["PhotoUrl"]?.ToString() : null
                };
            }
            else
            {
                vm.Profile = new EmployeeProfileViewModel
                {
                    EmployeeId = resolvedEmployeeId,
                    FullName = "Employee",
                    Email = "unknown@example.com"
                };
            }

            // Load leave requests
            var q = _db.Collection("LeaveRequests").WhereEqualTo("EmployeeId", resolvedEmployeeId);
            var snapshot = await q.GetSnapshotAsync();


            foreach (var doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();

                //DateTime start = dict.ContainsKey("StartDate") && dict["StartDate"] is Timestamp ts1 ? ts1.ToDateTime() : DateTime.MinValue;
                //DateTime end = dict.ContainsKey("EndDate") && dict["EndDate"] is Timestamp ts2 ? ts2.ToDateTime() : DateTime.MinValue;

                DateTime start = dict.ContainsKey("StartDate") && dict["StartDate"] is Timestamp ts1 ? ts1.ToDateTime().Date : DateTime.MinValue;
                DateTime end = dict.ContainsKey("EndDate") && dict["EndDate"] is Timestamp ts2 ? ts2.ToDateTime().Date : DateTime.MinValue;


                // Status from Firestore (fall back to Pending)
                string status = dict.ContainsKey("Status") ? dict["Status"]?.ToString() : StatusValues.Pending;

                // SupervisorApproved might be stored as bool; be defensive
                bool supervisorApproved = false;
                if (dict.ContainsKey("SupervisorApproved"))
                {
                    var val = dict["SupervisorApproved"];
                    if (val is bool b) supervisorApproved = b;
                    else if (bool.TryParse(val?.ToString() ?? "false", out var parsed)) supervisorApproved = parsed;
                }

                vm.MyLeaves.Add(new EmployeeLeaveViewModel
                {
                    leaveRequestId = doc.Id,
                    LeaveType = dict.ContainsKey("LeaveType") ? dict["LeaveType"]?.ToString() : "",
                    StartDate = start,
                    EndDate = end,
                    Status = status,
                    Reason = dict.ContainsKey("Reason") ? dict["Reason"]?.ToString() : "",
                    DocumentUrl = dict.ContainsKey("DocumentUrl") ? dict["DocumentUrl"]?.ToString() : null,
                    SupervisorApproved = supervisorApproved
                });
            }


            // --- counts ---
            // normalize comparisons using StatusValues
            vm.AnnualCount = vm.MyLeaves.Count(x => string.Equals(x.LeaveType ?? "", "Annual", StringComparison.OrdinalIgnoreCase));
            vm.SickCount = vm.MyLeaves.Count(x => string.Equals(x.LeaveType ?? "", "Sick", StringComparison.OrdinalIgnoreCase));
            vm.StudyCount = vm.MyLeaves.Count(x => string.Equals(x.LeaveType ?? "", "Study", StringComparison.OrdinalIgnoreCase));
            vm.FamilyCount = vm.MyLeaves.Count(x => !string.IsNullOrEmpty(x.LeaveType) &&
                                  new[] { "Family Responsibility", "FamilyResponsibility", "Family" }
                                  .Any(n => string.Equals(x.LeaveType, n, StringComparison.OrdinalIgnoreCase)));


            // IMPORTANT: PendingCount now includes both Pending and PendingHod
            vm.PendingCount = vm.MyLeaves.Count(x =>
                string.Equals(x.Status ?? "", StatusValues.Pending, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Status ?? "", StatusValues.PendingHod, StringComparison.OrdinalIgnoreCase)
            );

            // Compute summary counts
            vm.AnnualCount = vm.MyLeaves.Count(x => x.LeaveType == "Annual");
            vm.SickCount = vm.MyLeaves.Count(x => x.LeaveType == "Sick");
            vm.StudyCount = vm.MyLeaves.Count(x => x.LeaveType == "Study");
            vm.PendingCount = vm.MyLeaves.Count(x => x.Status == "Pending");
            vm.FamilyCount = vm.MyLeaves.Count(x => x.LeaveType == "Family Responsibility");

            // Order leaves by start date
            vm.MyLeaves = vm.MyLeaves.OrderByDescending(x => x.StartDate).ToList();

            //// Order leaves by start date
            //vm.MyLeaves = vm.MyLeaves.OrderByDescending(x => x.StartDate).ToList();

            // Pass email to view
            ViewBag.Email = HttpContext.Session.GetString("UserEmail");

            return View(vm);
        }
    }
}

