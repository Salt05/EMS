using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EMS.BlazorWASM;
using EMS.BlazorWASM.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ============ LOCAL STORAGE ============
builder.Services.AddBlazoredLocalStorage();

// ============ AUTHENTICATION ============
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

// ============ HTTP CLIENT with Auth Interceptor ============
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl")
    ?? "https://localhost:7001";

// Dynamically prefix subdomain to API base URL if present in current client host
var baseAddressUri = new Uri(builder.HostEnvironment.BaseAddress);
var clientHost = baseAddressUri.Host;
var parts = clientHost.Split('.');
if (parts.Length >= 2 && parts[0] != "localhost" && parts[0] != "ems")
{
    var subdomain = parts[0];
    var apiUriBuilder = new UriBuilder(apiBaseUrl);
    apiUriBuilder.Host = $"{subdomain}.{apiUriBuilder.Host}";
    apiBaseUrl = apiUriBuilder.ToString();
}

builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

// ============ SERVICES ============
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantServiceClient, TenantServiceClient>();
builder.Services.AddScoped<IEventServiceClient, EventServiceClient>();

await builder.Build().RunAsync();
