using System.Text;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Firebase.Auth;
using EMS.Infrastructure.Services;
using EMS.Infrastructure.Jobs;
using EMS.Core.Interfaces.Services;
using EMS.WebAPI.Middleware;
using EMS.WebAPI.Filters;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============ LOGGING ============
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/ems-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============ FIREBASE ADMIN SDK & FIRESTORE ============
var projectId = builder.Configuration["Firebase:ProjectId"] ?? "ems-project";
FirestoreDb firestoreDb;

var useEmulator = builder.Configuration.GetValue<bool>("Firebase:UseEmulator", false);

if (useEmulator && builder.Environment.IsDevelopment())
{
    // Force emulator hosts in development code-level fallback
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")))
    {
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");
    }
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST")))
    {
        Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", "localhost:9099");
    }

    // Dummy credentials to satisfy FirebaseAdmin SDK in local emulator
    FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
    {
        Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken("dummy-token"),
        ProjectId = projectId
    });

    firestoreDb = new FirestoreDbBuilder
    {
        ProjectId = projectId,
        EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly
    }.Build();
}
else
{
    var firebaseKeyFilePath = Path.Combine(builder.Environment.ContentRootPath, "firebase-key.json");
    if (File.Exists(firebaseKeyFilePath))
    {
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseKeyFilePath);
        FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions()
        {
            Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(firebaseKeyFilePath)
        });
    }
    else
    {
        FirebaseAdmin.FirebaseApp.Create();
    }

    firestoreDb = FirestoreDb.Create(projectId);
}

builder.Services.AddSingleton(FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance);
builder.Services.AddSingleton(firestoreDb);

// ============ FIREBASE CLIENT AUTH ============
var firebaseApiKey = builder.Configuration["Firebase:ApiKey"] ?? throw new ArgumentException("Firebase:ApiKey must be set");
var firebaseAuthDomain = builder.Configuration["Firebase:AuthDomain"] ?? throw new ArgumentException("Firebase:AuthDomain must be set");
var firebaseAuthClient = new FirebaseAuthClient(new FirebaseAuthConfig { 
    ApiKey = firebaseApiKey,
    AuthDomain = firebaseAuthDomain
});
builder.Services.AddSingleton(firebaseAuthClient);

// ============ SERVICES ============
builder.Services.AddScoped<IAuthService, FirebaseAuthService>();
builder.Services.AddScoped<IUserService, FirestoreUserService>();
builder.Services.AddScoped<ITenantService, FirestoreTenantService>();
builder.Services.AddScoped<IEventService, FirestoreEventService>();
builder.Services.AddScoped<IRegistrationService, FirestoreRegistrationService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITenantResolver, TenantResolver>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAdminUserService, FirestoreAdminUserService>();
builder.Services.AddScoped<EventReminderJob>();

// ============ HANGFIRE (background jobs) ============
// In-memory storage: the project uses Firestore, so there is no relational DB for Hangfire.
// Jobs are not persisted across restarts, which is fine for the recurring reminder schedule.
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());
builder.Services.AddHangfireServer();

// ============ HTTP CONTEXT ============
builder.Services.AddHttpContextAccessor();

// ============ JWT AUTHENTICATION ============
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ems";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ems-users";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ============ CONTROLLERS & SWAGGER ============
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token here. Don't include 'Bearer' prefix. E.g.: eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============ CORS ============
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// ============ MIDDLEWARE ============
app.UseMiddleware<TenantMiddleware>();

// ============ SWAGGER ============
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============ ROUTING & AUTH ============
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserStatusMiddleware>();  // Block inactive users even with valid JWT
app.MapControllers();

// Hangfire Dashboard — restrict access in non-Development environments.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = app.Environment.IsDevelopment()
        ? Array.Empty<IDashboardAuthorizationFilter>()        // open in dev
        : new[] { new HangfireAdminAuthorizationFilter() }    // restricted in prod
});

// Email reminders for events starting within the next 24h — runs hourly.
RecurringJob.AddOrUpdate<EventReminderJob>(
    "event-reminders",
    job => job.SendUpcomingEventRemindersAsync(),
    Cron.Hourly);

app.Run();

