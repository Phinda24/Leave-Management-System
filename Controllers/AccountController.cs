using Google.Cloud.Firestore;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Leave_Management_system.Controllers
{
    public class EmployeeAuthController : Controller
    {
        private readonly FirestoreDb _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public EmployeeAuthController(
            FirestoreDb db,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // =======================
        // GET: Login
        // =======================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // =======================
        // POST: Login
        // =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Please enter both Email and Password.");
                return View();
            }

            // 🔑 Firebase REST API sign-in
            var apiKey = _configuration["Firebase:WebConfig:apiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("Firebase API key is missing.");
            }

            var url =
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Invalid login details.");
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            string idToken = result.RootElement.GetProperty("idToken").GetString()!;
            string uid = result.RootElement.GetProperty("localId").GetString()!;
            string userEmail = result.RootElement.GetProperty("email").GetString()!;

            // ✅ Verify Firebase ID token
            await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

            // 🔍 Load employee profile
            var employeeDoc = await _db
                .Collection("Employees")
                .Document(uid)
                .GetSnapshotAsync();

            if (!employeeDoc.Exists)
            {
                ModelState.AddModelError("", "Employee profile not found.");
                return View();
            }

            string role = employeeDoc.ContainsField("Role")
                ? employeeDoc.GetValue<string>("Role")
                : "Employee";

            string fullName = employeeDoc.ContainsField("FullName")
                ? employeeDoc.GetValue<string>("FullName")
                : "";

            // =======================
            // 🔐 SESSION ()
            // =======================
            HttpContext.Session.SetString("UID", uid);
            HttpContext.Session.SetString("UserEmail", userEmail);
            HttpContext.Session.SetString("Email", userEmail);
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("Role", role);
            HttpContext.Session.SetString("FullName", fullName);
            HttpContext.Session.SetString("EmployeeId", uid);

            // =======================
            // 🔐 ASP.NET COOKIE AUTH (FIX)
            // =======================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, uid),
                new Claim(ClaimTypes.Email, userEmail),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            // =======================
            // 🔁 Redirect
            // =======================
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return Redirect("/EmployeeHome/Index");
        }

        // =======================
        // POST: Logout
        // =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}


