namespace EMS.Core.Entities.Enums;

public enum EventScope
{
    Public = 0, // Everyone sees and registers
    Internal = 1, // Only tenant members see and register
    PublicViewTenantRegister = 2, // Everyone sees, only tenant registers
    TenantViewOnly = 3, // Only tenant members see, no one registers
    Hidden = 4 // Hidden from everyone
}
