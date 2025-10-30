using Google.Cloud.Firestore;
using Leave_Management_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace Leave_Management.Controllers
{
    public class ApplyLeaveController : Controller
    {
        private readonly FirestoreDb _db;

        public ApplyLeaveController(FirestoreDb db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index(string employeeId)
        {
            ViewBag.EmployeeId = employeeId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LeaveRequest request)
        {
            if (!ModelState.IsValid)
                return View(request);

            await _db.Collection("LeaveRequests").AddAsync(request);

            TempData["Message"] = "Leave application submitted successfully!";
            return RedirectToAction("Index", "LeaveApplications", new { employeeId = request.EmployeeId });
        }
    }
}

