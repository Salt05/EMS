using System.Collections.Generic;
using System.Threading.Tasks;
using EMS.Shared.DTOs;

namespace EMS.Core.Interfaces.Services;

public interface ISuperAdminManagementService
{
    Task<List<TenantDTO>> GetTenantsWithStatsAsync();
}
