namespace EMS.Mvc.Services
{
    public interface IUserContext
    {
        string? DisplayName { get; }
        string? UserEmail { get; }
        string? UserRole { get; }
        bool IsLoggedIn { get; }
        void SetSession(string displayName, string email, string role);
        void ClearSession();
    }
}
