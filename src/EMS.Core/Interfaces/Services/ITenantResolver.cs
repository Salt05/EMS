namespace EMS.Core.Interfaces.Services;

public interface ITenantResolver
{
    string? ResolveTenantFromHost(string host);
    string? ResolveTenantIdFromContext();
}
