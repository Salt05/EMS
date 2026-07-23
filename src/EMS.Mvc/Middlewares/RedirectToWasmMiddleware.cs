using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EMS.Mvc.Middlewares
{
    public class RedirectToWasmMiddleware
    {
        private readonly RequestDelegate _next;

        public RedirectToWasmMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "/";

            // If it's a request to old auth controllers, redirect to unified Blazor login/register/logout pages
            if (path.Equals("/Auth/Login", System.StringComparison.OrdinalIgnoreCase))
            {
                var queryString = context.Request.QueryString.Value ?? "";
                context.Response.Redirect($"/login{queryString}");
                return;
            }
            if (path.Equals("/Auth/Register", System.StringComparison.OrdinalIgnoreCase))
            {
                var queryString = context.Request.QueryString.Value ?? "";
                context.Response.Redirect($"/register{queryString}");
                return;
            }
            if (path.Equals("/Auth/Logout", System.StringComparison.OrdinalIgnoreCase))
            {
                // Clear the MVC local cookie session with matching options
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = context.Request.IsHttps,
                    Path = "/"
                };
                context.Response.Cookies.Delete("user_session", cookieOptions);
                context.Response.Cookies.Delete("user_session");

                // Sign out ASP.NET authentication scheme
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    context, 
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

                // Prevent response caching
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";

                context.Response.Redirect("/logout");
                return;
            }

            await _next(context);
        }
    }
}
