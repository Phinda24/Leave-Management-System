using Google.Cloud.Firestore;
using Leave_Management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Leave_Management_system.Controllers
{
    public class HodHomeController : Controller
    {
        private readonly FirestoreDb _db;
        private readonly IHttpContextAccessor _http;

        public HodHomeController(FirestoreDb db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _http = httpContextAccessor;
        }

        // Compatibility session helper (same idea used in LeaveRequestController)
        private string? GetSessionValue(string key)
        {
            var v = _http.HttpContext?.Session?.GetString(key);
            if (!string.IsNullOrEmpty(v)) return v;

            var fallbacks = new[] { "HodID", "EmployeeId", "UserId", "Uid", "Email" };
            foreach (var k in fallbacks)
            {
                var val = _http.HttpContext?.Session?.GetString(k);
                if (!string.IsNullOrEmpty(val)) return val;
            }
            return null;
        }

        // GET: /HodHome/Index
        public async Task<IActionResult> Index()
        {
            // require HOD role (safe-guard)
            var role = _http.HttpContext?.Session?.GetString("Role");
            if (string.IsNullOrEmpty(role) || !role.Equals("HOD", StringComparison.OrdinalIgnoreCase))
            {
                // Not signed in as HOD -> redirect to HOD login
                return RedirectToAction("Login", "HodAuth");
            }

            try
            {
                // fetch pending leave requests (adjust query if you want department-specific)
                QuerySnapshot snapshot = await _db.Collection("LeaveRequests")
                                                  .WhereEqualTo("Status", "Pending")
                                                  .GetSnapshotAsync();

                var requests = snapshot.Documents.Select(d => d.ConvertTo<LeaveRequest>()).ToList();
                return View(requests); // expects Views/HodHome/Index.cshtml
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading HOD dashboard: {ex.Message}";
                return View(new List<LeaveRequest>());
            }
        }

        public async Task<IActionResult> HodDashboard()
        {
            // Ensure caller is Supervisor (optional; improve security)
            var role = _http?.HttpContext?.Session?.GetString("Role") ?? HttpContext?.Session?.GetString("Role");
            if (string.IsNullOrEmpty(role) || !role.Equals("HOD", System.StringComparison.OrdinalIgnoreCase))
            {
                // Redirect to supervisor login or access denied page if you have one
                return RedirectToAction("Login", "HodAuth");
            }

            if (_db == null)
            {
                TempData["ErrorMessage"] = "Database not available.";
                // return empty model so the view still renders
                return View(new List<LeaveRequest>());
            }

            // Query requests awaiting supervisor decision.
            // Match the same status string you use when creating requests; your other code used "PendingSupervisor".
            var snapshot = await _db.Collection("LeaveRequests")
                                    .WhereEqualTo("Status", "Pending")
                                    .GetSnapshotAsync();

            var list = snapshot.Documents
                               .Select(d => {
                                   var lr = d.ConvertTo<LeaveRequest>();
                                   lr.Id = d.Id; // populate Id for forms/links
                                   return lr;
                               })
                               .ToList();

            return View(list); // resolves to Views/SupHome/SupDashboard.cshtml
        }
    }
}

