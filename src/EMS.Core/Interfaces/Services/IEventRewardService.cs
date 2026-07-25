using EMS.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Core.Interfaces.Services;

public interface IEventRewardService
{
    Task<List<EventReward>> GetRewardsByEventAsync(string eventId, string tenantId);
    Task<bool> SaveEventRewardsAsync(string eventId, string tenantId, List<EventReward> rewards);
    Task<bool> DeleteRewardAsync(string rewardId, string tenantId);
}
