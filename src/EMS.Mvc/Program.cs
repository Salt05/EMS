using Google.Cloud.Firestore;
using EMS.Infrastructure.Services;
using EMS.Core.Interfaces.Services;
using EMS.Mvc.Middlewares;
using EMS.Mvc.Services;
using EMS.BlazorWASM.Services;
using Blazored.LocalStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var useInMemoryData = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue("Development:UseInMemoryData", true);

if (useInMemoryData)
{
    builder.Services.AddSingleton<IEventService, DevInMemoryEventService>();
    builder.Services.AddSingleton<ITenantService, DevInMemoryTenantService>();
    builder.Services.AddSingleton<IRegistrationService, DevInMemoryRegistrationService>();
    builder.Services.AddSingleton<IAgendaService, DevInMemoryAgendaService>();
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
    builder.Services.AddScoped<IAgendaService, FirestoreAgendaService>();
}

// ============ SHARED SERVICES ============
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantResolver, TenantResolver>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IUserContext, UserContext>();

// ============ COOKIE AUTHENTICATION ============
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// ============ BLAZOR SERVER AUTH & ADMIN SERVICES ============
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, CookieAuthenticationStateProvider>();

// Blazored.LocalStorage (required by WASM components, no-op server-side but satisfies DI)
builder.Services.AddBlazoredLocalStorage();

// HttpClient for WASM service clients to call WebAPI from server-side
var webApiBaseUrl = builder.Configuration["WebApiBaseUrl"] ?? "https://localhost:7296";
builder.Services.AddScoped<ServerAuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ServerAuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(webApiBaseUrl)
    };
});

// Register all Blazor WASM service clients for Admin Dashboard
builder.Services.AddScoped<EMS.BlazorWASM.Services.IAuthService, ServerAuthService>();
builder.Services.AddScoped<ITenantServiceClient, ServerTenantServiceClient>();
builder.Services.AddScoped<IEventServiceClient, EventServiceClient>();
builder.Services.AddScoped<IRegistrationServiceClient, RegistrationServiceClient>();
builder.Services.AddScoped<ICheckInServiceClient, CheckInServiceClient>();
builder.Services.AddScoped<IAdminUserServiceClient, AdminUserServiceClient>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ISuperAdminServiceClient, SuperAdminServiceClient>();
builder.Services.AddScoped<ITenantAdminServiceClient, TenantAdminServiceClient>();
builder.Services.AddScoped<IOrganizerServiceClient, OrganizerServiceClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Serve BlazorWASM wwwroot files at /admin-assets so Admin host page can load CSS/JS
var blazorWasmWwwroot = Path.Combine(builder.Environment.ContentRootPath, "..", "EMS.BlazorWASM", "wwwroot");
if (Directory.Exists(blazorWasmWwwroot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetFullPath(blazorWasmWwwroot)),
        RequestPath = "/admin-assets"
    });
}

app.UseRouting();

app.UseMiddleware<RedirectToWasmMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Resolve tenant subdomain on every request
app.UseMiddleware<TenantMiddleware>();

app.MapBlazorHub();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
