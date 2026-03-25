using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using Leave_Management_system.Models;
using System;
using System.Collections.Generic;

namespace Leave_Management_system.Controllers
{
    public class HodAuthController : Controller
    {
        private readonly FirestoreDb _db;

        public HodAuthController(FirestoreDb db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Helper to get the first matching key from Firestore doc dictionary (case-aware variants)
        private string? GetFieldValue(Dictionary<string, object> dict, params string[] candidates)
        {
            foreach (var key in candidates)
            {
                if (dict.ContainsKey(key) && dict[key] != null)
                    return dict[key].ToString();
            }
            return null;
        }

        // Note: firebaseUid is optional. If provided, we look up by UID field (tries multiple name variants).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? firebaseUid = null, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(firebaseUid))
            {
                ModelState.AddModelError("", "Please enter Email (or login via Firebase).");
                return View();
            }

            DocumentSnapshot doc = null;
            Dictionary<string, object>? hodData = null;

            // 1) Try Firebase UID lookup (try multiple possible field names used in Firestore)
            if (!string.IsNullOrWhiteSpace(firebaseUid))
            {
                // Try common variants for the UID field (case-sensitive)
                var uidFields = new[] { "Uid", "UID", "uid", "FirebaseUid", "firebaseUid" };
                foreach (var field in uidFields)
                {
                    var qByUid = await _db.Collection("HODs")
                                          .WhereEqualTo(field, firebaseUid)
                                          .Limit(1)
                                          .GetSnapshotAsync();

                    if (qByUid.Count > 0)
                    {
                        doc = qByUid.Documents.First();
                        break;
                    }
                }
            }

            // 2) Fallback to Email+Password lookup (existing behavior)
            if (doc == null)
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("", "Password is required for email login.");
                    return View();
                }

                var query = await _db.Collection("HODs")
                    .WhereEqualTo("Email", email)
                    .WhereEqualTo("Password", password)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (query.Count > 0)
                {
                    doc = query.Documents.First();
                }
            }

            if (doc != null && doc.Exists)
            {
                hodData = doc.ToDictionary();

                // Read fields using multiple candidate names (to be robust against case differences)
                string hodId = GetFieldValue(hodData, "HodID", "HODID", "hodId") ?? doc.Id;
                string fullName = GetFieldValue(hodData, "FullName", "Name", "fullName") ?? string.Empty;
                string storedEmail = GetFieldValue(hodData, "Email", "email") ?? email;
                string storedUid = GetFieldValue(hodData, "Uid", "UID", "uid", "FirebaseUid", "firebaseUid") ?? string.Empty;

                // Set session values (these keys are used elsewhere in your app)
                HttpContext.Session.SetString("UserRole", "HOD");
                HttpContext.Session.SetString("Role", "HOD");
                HttpContext.Session.SetString("UserEmail", storedEmail);
                HttpContext.Session.SetString("Email", storedEmail);
                HttpContext.Session.SetString("FullName", fullName);
                HttpContext.Session.SetString("HodID", hodId);
                // also set EmployeeId so LeaveRequestController's GetSessionValue can pick it up
                HttpContext.Session.SetString("EmployeeId", hodId);

                if (!string.IsNullOrEmpty(storedUid))
                    HttpContext.Session.SetString("Uid", storedUid);

                // Redirect to HodHome/Index so the view at Views/HodHome/Index.cshtml is used
                return Redirect("/HodHome/HodDashboard");
            }

            ModelState.AddModelError("", "Invalid Email or Password, or user not found.");
            return View();
        }

        // POST: HodAuth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "HodAuth");
        }
    }
}

