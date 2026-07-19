using Microsoft.AspNetCore.Http;
using System;

namespace EMS.Mvc.Services
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CookieName = "user_session";

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private (string? displayName, string? email, string? role) GetParsedSession()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return (null, null, null);

            string? userSession = context.Request.Cookies[CookieName];
            if (string.IsNullOrEmpty(userSession))
            {
                return (null, null, null);
            }

            var parts = userSession.Split('|');
            if (parts.Length >= 3)
            {
                return (parts[0], parts[1], parts[2]);
            }

            return (null, null, null);
        }

        public string? DisplayName => GetParsedSession().displayName;
        public string? UserEmail => GetParsedSession().email;
        public string? UserRole => GetParsedSession().role;
        public bool IsLoggedIn => !string.IsNullOrEmpty(DisplayName);

        public void SetSession(string displayName, string email, string role)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddHours(1),
                SameSite = SameSiteMode.Lax,
                Secure = true // Enable secure cookie usage
            };

            context.Response.Cookies.Append(CookieName, $"{displayName}|{email}|{role}", cookieOptions);
        }

        public void ClearSession()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            context.Response.Cookies.Delete(CookieName);
        }
    }
}
