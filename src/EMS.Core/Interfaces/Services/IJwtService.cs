namespace EMS.Core.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(string userId, string email, string fullName, List<string> roles, string tenantId);
    (bool Valid, string UserId, string TenantId) ValidateToken(string token);
}
