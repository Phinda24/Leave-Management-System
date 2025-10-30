//using FirebaseAdmin;
//using Google.Api;
//using Google.Apis.Auth.OAuth2;
//using Google.Cloud.Firestore;
//using Google.Cloud.Firestore.V1;
//using Microsoft.AspNetCore.Authentication.Cookies; // Add this using directive

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//string pathToKey = Path.Combine(
//    builder.Environment.WebRootPath, // points to wwwroot
//    "secrets",
//    "serviceAccountKey.json"
//);
//builder.Services.AddControllersWithViews();
//builder.Services.AddApplicationInsightsTelemetry();

//// Session (required for HttpContext.Session usage)
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

////firebase Admin SDK
//FirebaseApp.Create(new AppOptions()
//{
//    Credential = GoogleCredential.FromFile(pathToKey),
//});

////firestore
//GoogleCredential credential = GoogleCredential.FromFile(pathToKey);
//// FIX: Create FirestoreClient from credential, then pass to FirestoreDb.Create
//FirestoreClientBuilder clientBuilder = new FirestoreClientBuilder
//{
//    Credential = credential
//};
//FirestoreClient firestoreClient = clientBuilder.Build();
//builder.Services.AddSingleton(provider =>
//    FirestoreDb.Create("leavemanagementsystem-9cd7d", firestoreClient)
//);

//// Authentication (cookie)
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/Account/Login";
//        options.LogoutPath = "/Account/Logout";
//        options.ExpireTimeSpan = TimeSpan.FromDays(14); // cookie lifetime for persistent login
//        options.SlidingExpiration = true;
//        options.Cookie.HttpOnly = true;
//        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
//        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // enable on prod
//    });

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//// Session must be enabled before MVC endpoints
//app.UseSession();



//// Authentication middleware
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.Run();
//// Please make sure that the secrets folder is correctly configured in your project settings, especially if you are using an IDE like Visual Studio.


//using FirebaseAdmin;
//using Google.Api;
//using Google.Apis.Auth.OAuth2;
//using Google.Cloud.Firestore;
//using Google.Cloud.Firestore.V1;
//using Leave_Management_system.Service;
//using Microsoft.AspNetCore.Authentication.Cookies;

//var builder = WebApplication.CreateBuilder(args);

//// -------------------------
//// Path to Service Account
//// -------------------------
//// IMPORTANT: use ContentRootPath (app root), NOT WebRootPath (wwwroot), otherwise
//// the key could be served publicly if misconfigured.

////string pathToKey = Path.Combine(
////    builder.Environment.ContentRootPath, // safer than WebRootPath
////    "secrets",                           // put serviceAccountKey.json under <project-root>/secrets
////    "serviceAccountKey.json"
////);

////// Optional: set GOOGLE_APPLICATION_CREDENTIALS env var (helpful for some Google libs/tools)
////// You can also set this in your hosting environment instead of in code.
/////
////if (File.Exists(pathToKey))
////{
////    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKey);
////}
////else
////{
////    // you may want to throw or log here in dev if the file is missing
////    // throw new FileNotFoundException("Firebase service account file not found", pathToKey);
////}
//string pathToKey = Path.Combine(
//    builder.Environment.ContentRootPath,
//    "serviceAccountKey.json"
//);
//FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(pathToKey) });

//if (!File.Exists(pathToKey))
//{ 
//    throw new FileNotFoundException($"Missing service account key. Expected at: {pathToKey}");
//}
//Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKey);


//builder.Services.AddControllersWithViews();
//builder.Services.AddApplicationInsightsTelemetry();

//// Session (required for HttpContext.Session usage)
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

//// -------------------------
//// Initialize Firebase Admin
//// -------------------------
//// Guard against creating multiple FirebaseApp instances (useful in dev/hot-reload).
//if (FirebaseApp.DefaultInstance == null)
//{
//    if (!File.Exists(pathToKey))
//    {
//        // In production you may prefer to rely on env var credentials rather than a file.
//        // For now leave a helpful message:
//        Console.WriteLine($"WARNING: Firebase service account file not found at: {pathToKey}");
//    }
//    else
//    {
//        FirebaseApp.Create(new AppOptions
//        {
//            Credential = GoogleCredential.FromFile(pathToKey)
//        });
//    }
//}

//// -------------------------
//// Firestore registration
//// -------------------------
//// Create FirestoreClient from the same credential (recommended)
//GoogleCredential credential = GoogleCredential.FromFile(pathToKey);
//FirestoreClientBuilder clientBuilder = new FirestoreClientBuilder
//{
//    Credential = credential
//};
//FirestoreClient firestoreClient = clientBuilder.Build();

//// Replace projectId with your real Firebase project id
//string projectId = "leavemanagementsystem-9cd7d";
//builder.Services.AddSingleton(provider =>
//    FirestoreDb.Create(projectId, firestoreClient)
//);

//// -------------------------
//// Register FcmService for DI
//// -------------------------
//builder.Services.AddSingleton<FcmService>();

//// Make sure you have the FcmService class (from earlier message) in the project and correct namespace.
//// The service depends on FirestoreDb which is already registered above.
////builder.Services.AddSingleton<Services.FcmService>(); // adjust namespace if needed

//// -------------------------
//// Authentication (cookie)
//// -------------------------
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/Account/Login";
//        options.LogoutPath = "/Account/Logout";
//        options.ExpireTimeSpan = TimeSpan.FromDays(14);
//        options.SlidingExpiration = true;
//        options.Cookie.HttpOnly = true;
//        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
//        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // enable on prod
//    });

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//// Session must be enabled before MVC endpoints
//app.UseSession();

//// Authentication middleware
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.Run();
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Leave_Management_system.Service;
using Microsoft.AspNetCore.Authentication.Cookies;



var builder = WebApplication.CreateBuilder(args);

// -------------------------
// Locate service account (prefer secrets/ folder)
// -------------------------
string secretsPath = Path.Combine(builder.Environment.ContentRootPath, "secrets", "serviceAccountKey.json");
string rootPath = Path.Combine(builder.Environment.ContentRootPath, "serviceAccountKey.json");

// prefer secrets path, then root path, otherwise null
string? pathToKey = File.Exists(secretsPath) ? secretsPath : (File.Exists(rootPath) ? rootPath : null);

// -------------------------
// Resolve Google credentials (file -> ADC fallback)
// -------------------------
GoogleCredential? googleCredential = null;
if (!string.IsNullOrEmpty(pathToKey))
{
    // Set env var (helpful for some Google libraries/tools)
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKey);

    // Load credential from file
    googleCredential = GoogleCredential.FromFile(pathToKey);
}
else
{
    // Attempt application default credentials (useful in GCP environments)
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
// Initialize Firebase Admin (if credential available)
// -------------------------
if (googleCredential != null && FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new FirebaseAdmin.AppOptions
    {
        Credential = googleCredential
    });
}

// -------------------------
// Add framework services
// -------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddApplicationInsightsTelemetry();

// Required by session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Needed by controllers/layouts that inject IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// -------------------------
// Firestore registration
// -------------------------
string projectId = "leavemanagementsystem-9cd7d";

if (googleCredential != null)
{
    // Create FirestoreClient from explicit credential
    var clientBuilder = new FirestoreClientBuilder { Credential = googleCredential };
    FirestoreClient firestoreClient = clientBuilder.Build();
    builder.Services.AddSingleton(provider => FirestoreDb.Create(projectId, firestoreClient));
}
else
{
    // Fallback: create default FirestoreDb (will use ADC if available in environment)
    builder.Services.AddSingleton(provider => FirestoreDb.Create(projectId));
}

// -------------------------
// Register FcmService for DI
// -------------------------
builder.Services.AddSingleton<FcmService>();

// -------------------------
// Authentication (cookie)
// -------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // enable on prod
    });

var app = builder.Build();

// -------------------------
// Configure HTTP request pipeline
// -------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session must be enabled before MVC endpoints
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();