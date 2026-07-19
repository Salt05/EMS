using Google.Cloud.Firestore;
using EMS.Infrastructure.Services;
using EMS.Core.Interfaces.Services;
using EMS.Mvc.Middlewares;
using EMS.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddServerSideBlazor();

var useInMemoryData = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue("Development:UseInMemoryData", true);

if (useInMemoryData)
{
    builder.Services.AddSingleton<IEventService, DevInMemoryEventService>();
    builder.Services.AddSingleton<ITenantService, DevInMemoryTenantService>();
    builder.Services.AddSingleton<IRegistrationService, DevInMemoryRegistrationService>();
}
else
{
    // ============ FIREBASE & FIRESTORE SETUP ============
    var projectId = builder.Configuration["Firebase:ProjectId"] ?? "digiems-5ef8a";
    FirestoreDb firestoreDb;

    var useEmulator = builder.Configuration.GetValue<bool>("Firebase:UseEmulator", false);

    if (useEmulator && builder.Environment.IsDevelopment())
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")))
        {
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");
        }
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST")))
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", "localhost:9099");
        }

        firestoreDb = new FirestoreDbBuilder
        {
            ProjectId = projectId,
            EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly
        }.Build();
    }
    else
    {
        // Clear emulator env vars if set by launchSettings.json so we connect to the cloud Firebase project
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", null);
        Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", null);

        var firebaseKeyFilePath = Path.Combine(builder.Environment.ContentRootPath, "firebase-key.json");
        if (File.Exists(firebaseKeyFilePath))
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseKeyFilePath);
        }
        firestoreDb = FirestoreDb.Create(projectId);
    }

    builder.Services.AddSingleton(firestoreDb);
    builder.Services.AddScoped<ITenantService, FirestoreTenantService>();
    builder.Services.AddScoped<IEventService, FirestoreEventService>();
    builder.Services.AddScoped<IRegistrationService, FirestoreRegistrationService>();
}

// ============ SHARED SERVICES ============
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantResolver, TenantResolver>();
builder.Services.AddScoped<IUserContext, UserContext>();

var app = builder.Build();

app.UseMiddleware<ProcessingTimeMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// Resolve tenant subdomain on every request
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.MapBlazorHub();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
