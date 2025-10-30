using Google.Cloud.Firestore;
using Leave_Management_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace Leave_Management.Controllers
{
    public class LeaveApplicationsController : Controller
    {
        private readonly FirestoreDb _db;

        public LeaveApplicationsController(FirestoreDb db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string employeeId)
        {
            var query = _db.Collection("LeaveRequest").WhereEqualTo("EmployeeId", employeeId);
            var snapshot = await query.GetSnapshotAsync();
            var applications = snapshot.Documents.Select(d => d.ConvertTo<LeaveRequest>()).ToList();

            return View(applications);
        }
    }
}
