
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Leave_Management_system.Service;
using Leave_Management_system.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// Locate service account
// -------------------------
string secretsPath = Path.Combine(builder.Environment.ContentRootPath, "secrets", "serviceAccountKey.json");
string rootPath = Path.Combine(builder.Environment.ContentRootPath, "serviceAccountKey.json");

string? pathToKey = File.Exists(secretsPath)
    ? secretsPath
    : (File.Exists(rootPath) ? rootPath : null);

// -------------------------
// Resolve Google credentials
// -------------------------
GoogleCredential? googleCredential = null;

if (!string.IsNullOrEmpty(pathToKey))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKey);
    googleCredential = GoogleCredential.FromFile(pathToKey);
}
else
{
    try
    {
        googleCredential = GoogleCredential.GetApplicationDefault();
    }
    catch
    {
        googleCredential = null;
    }
}

// -------------------------
// Initialize Firebase Admin
// -------------------------
if (googleCredential != null && FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new FirebaseAdmin.AppOptions
    {
        Credential = googleCredential
    });
}

// -------------------------
// Framework services
// -------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddApplicationInsightsTelemetry();

// 🔑 REQUIRED for Firebase REST API calls
builder.Services.AddHttpClient();

// -------------------------
// Session services
// -------------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Needed for HttpContext access
builder.Services.AddHttpContextAccessor();

// -------------------------
// Firestore registration
// -------------------------
string projectId = "leave-management-system-88d26";

builder.Services.AddSingleton<EmployeeLookupService>();

if (googleCredential != null)
{
    var clientBuilder = new FirestoreClientBuilder
    {
        Credential = googleCredential
    };

    FirestoreClient firestoreClient = clientBuilder.Build();
    builder.Services.AddSingleton(provider =>
        FirestoreDb.Create(projectId, firestoreClient));
}
else
{
    builder.Services.AddSingleton(provider =>
        FirestoreDb.Create(projectId));
}

// -------------------------
// FCM service
// -------------------------
builder.Services.AddSingleton<FcmService>(provider =>
    new FcmService(
        provider.GetRequiredService<FirestoreDb>(),
        provider.GetRequiredService<ILogger<FcmService>>()
    ));

// -------------------------
// Cookie Authentication
// -------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/EmployeeAuth/Login";
        options.LogoutPath = "/EmployeeAuth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // enable in prod
    });

var app = builder.Build();

// -------------------------
// HTTP request pipeline
// -------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔐 Session MUST come before auth
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
