using System.Text;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Firebase.Auth;
using EMS.Infrastructure.Services;
using EMS.Core.Interfaces.Services;
using EMS.WebAPI.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============ LOGGING ============
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/ems-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============ FIRESTORE ============
var projectId = builder.Configuration["Firebase:ProjectId"] ?? "ems-project";
var firestoreDb = new FirestoreClientBuilder { ProjectId = projectId }.Build();
builder.Services.AddSingleton(FirestoreDb.Create(projectId));

// ============ FIREBASE AUTH ============
var firebaseApiKey = builder.Configuration["Firebase:ApiKey"] ?? "";
var firebaseAuthClient = new FirebaseAuthClient(new FirebaseAuthConfig { ApiKey = firebaseApiKey });
builder.Services.AddSingleton(firebaseAuthClient);

// ============ SERVICES ============
builder.Services.AddScoped<IAuthService, FirebaseAuthService>();
builder.Services.AddScoped<IUserService, FirestoreUserService>();
builder.Services.AddScoped<ITenantService, FirestoreTenantService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITenantResolver, TenantResolver>();

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
builder.Services.AddSwaggerGen();

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
app.MapControllers();

app.Run();

