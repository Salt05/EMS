using EMS.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Jobs;

public class PaymentExpirationJob
{
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<PaymentExpirationJob> _logger;

    public PaymentExpirationJob(IRegistrationService registrationService, ILogger<PaymentExpirationJob> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    public async Task ExpirePendingPaymentsAsync()
    {
        var expiredCount = await _registrationService.ExpirePendingPaymentsAsync();
        if (expiredCount > 0)
            _logger.LogInformation("Expired {ExpiredCount} unpaid event registrations", expiredCount);
    }
}
