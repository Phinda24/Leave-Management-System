using Google.Cloud.Firestore;
using Leave_Management_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace Leave_Management.Controllers
{
    public class EmployeeDashboardController : Controller
    {
        private readonly FirestoreDb _db;

        public EmployeeDashboardController(FirestoreDb db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string employeeId)
        {
            // Get employee details
            var empSnapshot = await _db.Collection("Employees").Document(employeeId).GetSnapshotAsync();
            var employee = empSnapshot.Exists ? empSnapshot.ConvertTo<Employee>() : new Employee();

            // Get leave history
            var leaveQuery = _db.Collection("LeaveRequests").WhereEqualTo("EmployeeId", employeeId);
            var leaveSnapshot = await leaveQuery.GetSnapshotAsync();
            var leaveHistory = leaveSnapshot.Documents.Select(d => d.ConvertTo<LeaveRequest>()).ToList();

            ViewBag.Employee = employee;
            return View(leaveHistory);
        }
    }
}
