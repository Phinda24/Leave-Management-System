//using Google.Cloud.Firestore;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Http;
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Leave_Management_system.Models;



//namespace Leave_Management_system.Controllers
//{
//    public class EmployeeAuthController : Controller
//    {
//        private readonly FirestoreDb _db;

//        public EmployeeAuthController(FirestoreDb db)
//        {
//            _db = db;
//        }

//        // GET: EmployeeAuth/Login
//        [HttpGet]
//        public IActionResult Login(string? returnUrl = null)
//        {
//            ViewData["ReturnUrl"] = returnUrl;
//            return View();
//        }

//        // POST: EmployeeAuth/Login
//        [HttpPost]
//        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
//        {
//            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
//            {
//                ModelState.AddModelError("", "Please enter both Email and Password.");
//                return View();
//            }

//            // 🔹 Fetch all employees (needed for case-insensitive email check)
//            var querySnapshot = await _db.Collection("Employees").GetSnapshotAsync();

//            // 🔹 Find matching employee by email (case-insensitive, trimmed)
//            var employeeDoc = querySnapshot.Documents
//                .Select(d => new { Doc = d, Data = d.ToDictionary() })
//                .FirstOrDefault(x =>
//                    x.Data.ContainsKey("EmployeeID") &&
//                    string.Equals(x.Data["EmployeeID"]?.ToString()?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase)
//                );

//            if (employeeDoc != null)
//            {

//                // 🔹 Compare password (trimmed)
//                string storedPassword = employeeDoc.Data["Password"]?.ToString()?.Trim();
//                if (storedPassword == password.Trim())
//                {

//                    // ✅ Login success, set session
//                    HttpContext.Session.SetString("EmployeeId", employeeDoc.Doc.Id); // Firestore doc ID (e.g., Emp001)
//                    HttpContext.Session.SetString("UserEmail", employeeDoc.Data["EmployeeID"].ToString());
//                    HttpContext.Session.SetString("UserRole", "Employee");

//                    // Redirect to returnUrl if provided
//                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
//                        return Redirect(returnUrl);

//                    // Default redirect
//                    return RedirectToAction("Index", "EmployeeHome");

//                }
//            }

//            // 🔹 Wrong credentials
//            ModelState.AddModelError("", "Incorrect Email or Password.");
//            return View();


//        }



//        //[HttpPost]
//        //[ValidateAntiForgeryToken]
//        //public async Task<IActionResult> UnregisterFcmToken([FromForm] string token)
//        //{
//        //    var employeeId = HttpContext.Session.GetString("EmployeeId");
//        //    if (string.IsNullOrEmpty(employeeId)) return Unauthorized();

//        //    var docRef = _db.Collection("Employees").Document(employeeId);
//        //    await docRef.UpdateAsync(new Dictionary<string, object>
//        //    {
//        //{ "FcmTokens", FieldValue.ArrayRemove(token) }
//        //    });

//        //    return Ok();
//        //}

//        // POST: EmployeeAuth/Logout
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult Logout()
//        {
//            HttpContext.Session.Clear();
//            return RedirectToAction("Login", "EmployeeAuth");
//        }


//        //public class TokenDto { public string Token { get; set; } }

//        //[HttpPost]
//        //[ValidateAntiForgeryToken]
//        //public async Task<IActionResult> RegisterFcmToken([FromForm] string token)
//        //{

//        //    if (string.IsNullOrEmpty(token)) return BadRequest("Token missing.");

//        //    // Prefer these session keys (adjust names to your app)
//        //    var employeeId = HttpContext.Session.GetString("EmployeeId");
//        //    var supId = HttpContext.Session.GetString("SupID");
//        //    var hodId = HttpContext.Session.GetString("HodID");

//        //    string userId = employeeId ?? supId ?? hodId;
//        //    if (string.IsNullOrEmpty(userId)) return Unauthorized("Not logged in.");

//        //    var docRef = _db.Collection("Employees").Document(userId); // keep single collection for all user types; adjust if you use separate collections
//        //    if (!string.IsNullOrEmpty(supId)) docRef = _db.Collection("Supervisors").Document(supId);
//        //    else if (!string.IsNullOrEmpty(hodId)) docRef = _db.Collection("Hods").Document(hodId);
//        //    else docRef = _db.Collection("Employees").Document(employeeId);

//        //    try
//        //    {
//        //        await docRef.UpdateAsync(new Dictionary<string, object>
//        //{
//        //    { "FcmTokens", FieldValue.ArrayUnion(token) }
//        //});
//        //        return Ok(new { success = true });
//        //    }
//        //    catch (Grpc.Core.RpcException rex) when (rex.Status.StatusCode == Grpc.Core.StatusCode.NotFound)
//        //    {
//        //        // Optionally create the doc or return NotFound
//        //        return NotFound("User document not found.");
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        // log in prod
//        //        return StatusCode(500, ex.Message);
//        //    }
//        //}


//    }


//}


using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Leave_Management_system.Models;
using BCrypt.Net;

namespace Leave_Management_system.Controllers
{
    public class EmployeeAuthController : Controller
    {
        private readonly FirestoreDb _db;

        public EmployeeAuthController(FirestoreDb db)
        {
            _db = db;
        }

        // GET: EmployeeAuth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: EmployeeAuth/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Please enter both Email and Password.");
                return View();
            }

            try
            {
                var trimmed = email.Trim();
                // Prefer to store a normalized email in Firestore for exact queries (e.g., "NormalizedEmail")
                // This query looks for exact match on EmployeeID (adjust if your field is different)
                QuerySnapshot snapshot = await _db.Collection("Employees")
                    .WhereEqualTo("EmployeeID", trimmed)
                    .Limit(1)
                    .GetSnapshotAsync();

                var doc = snapshot.Documents.FirstOrDefault();
                if (doc != null)
                {
                    var data = doc.ToDictionary();
                    var storedPasswordObj = doc.ContainsField("Password") ? doc.GetValue<object>("Password") : null;
                    string stored = storedPasswordObj?.ToString()?.Trim() ?? "";

                    bool passwordOk = false;

                    // If stored password is a bcrypt hash, verify with BCrypt
                    if (!string.IsNullOrEmpty(stored) && (stored.StartsWith("$2a$") || stored.StartsWith("$2b$") || stored.StartsWith("$2y$")))
                    {
                        passwordOk = BCrypt.Net.BCrypt.Verify(password, stored);
                    }
                    else
                    {
                        // fallback: plain-text comparison (legacy). Consider migrating to hashed.
                        passwordOk = stored == password.Trim();

                        // Optionally, on successful plaintext login, replace stored password with a bcrypt hash.
                        if (passwordOk)
                        {
                            var newHash = BCrypt.Net.BCrypt.HashPassword(password.Trim());
                            await _db.Collection("Employees").Document(doc.Id).UpdateAsync(new Dictionary<string, object>
                            {
                                { "Password", newHash }
                            });
                        }
                    }

                    if (passwordOk)
                    {
                        HttpContext.Session.SetString("EmployeeId", doc.Id);
                        HttpContext.Session.SetString("UserEmail", trimmed);
                        HttpContext.Session.SetString("UserRole", "Employee");
                        // other session props if available
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            return Redirect(returnUrl);
                        return RedirectToAction("Index", "EmployeeHome");
                    }
                }

                ModelState.AddModelError("", "Incorrect Email or Password.");
                return View();
            }
            catch (Exception ex)
            {
                // log in production
                ModelState.AddModelError("", $"Login failed: {ex.Message}");
                return View();
            }
        }

        // POST: EmployeeAuth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "EmployeeAuth");
        }
    }
}
