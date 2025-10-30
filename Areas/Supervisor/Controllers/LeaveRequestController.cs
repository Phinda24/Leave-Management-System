using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace Leave_Management_system.Areas.Supervisor.Controllers
{
    [Area("Supervisor")]
    public class LeaveRequestController : Controller
    {
        private readonly FirestoreDb _db;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeaveRequestController(FirestoreDb db, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }
        // GET: /HOD/LeaveRequest/Pending
        public async Task<IActionResult> Pending()
        {
            var query = _db.Collection("LeaveRequests").WhereEqualTo("Status", "Pending");
            var snapshot = await query.GetSnapshotAsync();
            var list = snapshot.Documents.Select(d => d.ConvertTo<Leave_Management_system.Models.LeaveRequest>()).ToList();

            // decide layout from session/role
            var role = HttpContext.Session.GetString("UserRole"); // "Supervisor" or "HOD"
            ViewData["Layout"] = role == "HOD"
                ? "~/Views/Shared/_HodLayout.cshtml"
                : "~/Views/Shared/_SupLayout.cshtml";

            if (role == "HOD")
                return View("PendingHod", list);
            else
                return View("PendingSupervisor", list);

        }
    }
}
