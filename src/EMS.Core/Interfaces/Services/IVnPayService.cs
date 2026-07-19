namespace EMS.Core.Interfaces.Services;

public interface IVnPayService
{
    string CreatePaymentUrl(string ipAddress, string registrationId, decimal amount, string eventTitle, string returnUrl);
    bool ValidateCallback(Dictionary<string, string> queryParameters);
}
