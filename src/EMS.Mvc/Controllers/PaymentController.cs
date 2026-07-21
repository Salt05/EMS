using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Mvc.Controllers;

public class PaymentController : Controller
{
    private readonly IRegistrationService _registrationService;
    private readonly IEventService _eventService;
    private readonly IVnPayService _vnPayService;
    private readonly IUserContext _userContext;
    private readonly ILogger<PaymentController> _logger;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public PaymentController(
        IRegistrationService registrationService,
        IEventService eventService,
        IVnPayService vnPayService,
        IUserContext userContext,
        ILogger<PaymentController> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _registrationService = registrationService;
        _eventService = eventService;
        _vnPayService = vnPayService;
        _userContext = userContext;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Pay(string registrationId)
    {
        if (string.IsNullOrEmpty(registrationId))
        {
            TempData["ErrorMessage"] = "Mã đăng ký không hợp lệ.";
            return RedirectToAction("Index", "Home");
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? "tenant-1";
        var reg = await _registrationService.GetRegistrationByIdAsync(registrationId, tenantId);

        if (reg == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đăng ký.";
            return RedirectToAction("Index", "Home");
        }

        if (!_userContext.IsLoggedIn || !string.Equals(reg.StudentEmail, _userContext.UserEmail, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (reg.Status != RegistrationStatus.PendingPayment ||
            (reg.PaymentExpiresAt.HasValue && reg.PaymentExpiresAt.Value <= DateTime.UtcNow))
        {
            TempData["ErrorMessage"] = "Phiên giữ chỗ đã hết hạn. Vui lòng đăng ký lại để thanh toán.";
            return RedirectToAction("Detail", "Events", new { id = reg.EventId });
        }

        var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
        if (ev == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sự kiện tương ứng.";
            return RedirectToAction("Index", "Home");
        }

        if (ev.IsFree || ev.Price <= 0)
        {
            TempData["ErrorMessage"] = "Sự kiện này miễn phí, không cần thanh toán.";
            return RedirectToAction("Detail", "Events", new { id = ev.Id });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        
        // Build absolute return URL
        var returnUrl = Url.Action("Callback", "Payment", null, Request.Scheme) ?? "";

        _logger.LogInformation($"Initiating VNPAY payment for Registration: {registrationId}, Event: {ev.Title}, Amount: {ev.Price}");
        
        var paymentUrl = _vnPayService.CreatePaymentUrl(ipAddress, registrationId, ev.Price, ev.Title, returnUrl);
        return Redirect(paymentUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Callback()
    {
        var queryParameters = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        
        var isValidSignature = _vnPayService.ValidateCallback(queryParameters);
        if (!isValidSignature)
        {
            _logger.LogWarning("VNPAY Callback received invalid signature.");
            TempData["ErrorMessage"] = "Chữ ký thanh toán không hợp lệ hoặc đã bị thay đổi.";
            return RedirectToAction("Index", "Home");
        }

        queryParameters.TryGetValue("vnp_TxnRef", out var txnRef);
        queryParameters.TryGetValue("vnp_ResponseCode", out var responseCode);
        queryParameters.TryGetValue("vnp_TransactionStatus", out var transactionStatus);

        if (string.IsNullOrEmpty(txnRef))
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin giao dịch.";
            return RedirectToAction("Index", "Home");
        }

        // TxtRef is format: {registrationId}-{ticks} or {registrationId}_{ticks} where registrationId is a GUID
        var lastDash = txnRef.LastIndexOf('-');
        var lastUnderscore = txnRef.LastIndexOf('_');
        var sepIndex = Math.Max(lastDash, lastUnderscore);
        var registrationId = sepIndex > 0 ? txnRef.Substring(0, sepIndex) : txnRef;
        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? "tenant-1";
        var reg = await _registrationService.GetRegistrationByIdAsync(registrationId, tenantId);
        
        if (reg == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đăng ký tương ứng với giao dịch.";
            return RedirectToAction("Index", "Home");
        }

        if (responseCode == "00" && transactionStatus == "00")
        {
            queryParameters.TryGetValue("vnp_Amount", out var vnpAmount);
            queryParameters.TryGetValue("vnp_TransactionNo", out var transactionNo);
            decimal amount = decimal.TryParse(vnpAmount, out var parsedAmount) ? parsedAmount / 100m : 0;
            decimal feePercentage = 5m; // 5% platform fee

            // Payment success - mark registration as paid
            var success = await _registrationService.MarkAsPaidAsync(registrationId, tenantId, transactionNo ?? "", amount, feePercentage);
            if (success)
            {
                _logger.LogInformation($"VNPAY payment success for Registration: {registrationId}. Approved.");
                TempData["SuccessMessage"] = "Thanh toán thành công! Vé tham gia sự kiện của bạn đã được kích hoạt.";
                
                // Trigger SignalR Notification
                TriggerNotification(reg.EventId, tenantId, registrationId);
            }
            else
            {
                _logger.LogError($"Failed to approve registration {registrationId} after successful VNPAY payment.");
                TempData["ErrorMessage"] = "Thanh toán thành công nhưng không thể cập nhật vé. Vui lòng liên hệ Ban tổ chức.";
            }
        }
        else
        {
            _logger.LogWarning($"VNPAY payment failed for Registration: {registrationId}. ResponseCode: {responseCode}");
            TempData["ErrorMessage"] = $"Thanh toán thất bại hoặc đã bị hủy (Mã lỗi: {responseCode}).";
        }

        return RedirectToAction("Detail", "Events", new { id = reg.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Ipn()
    {
        var queryParameters = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        
        var isValidSignature = _vnPayService.ValidateCallback(queryParameters);
        if (!isValidSignature)
        {
            return Json(new { RspCode = "97", Message = "Invalid signature" });
        }

        queryParameters.TryGetValue("vnp_TxnRef", out var txnRef);
        queryParameters.TryGetValue("vnp_ResponseCode", out var responseCode);
        queryParameters.TryGetValue("vnp_TransactionStatus", out var transactionStatus);

        if (string.IsNullOrEmpty(txnRef))
        {
            return Json(new { RspCode = "01", Message = "Order not found" });
        }

        var lastDashIndex = txnRef.LastIndexOf('-');
        var lastUnderscoreIndex = txnRef.LastIndexOf('_');
        var separatorIndex = Math.Max(lastDashIndex, lastUnderscoreIndex);
        var registrationId = separatorIndex > 0 ? txnRef.Substring(0, separatorIndex) : txnRef;
        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? "tenant-1";
        var reg = await _registrationService.GetRegistrationByIdAsync(registrationId, tenantId);

        if (reg == null)
        {
            return Json(new { RspCode = "01", Message = "Order not found" });
        }

        if (reg.Status == RegistrationStatus.Approved)
        {
            return Json(new { RspCode = "02", Message = "Order already confirmed" });
        }

        if (responseCode == "00" && transactionStatus == "00")
        {
            queryParameters.TryGetValue("vnp_Amount", out var vnpAmount);
            queryParameters.TryGetValue("vnp_TransactionNo", out var transactionNo);
            decimal amount = decimal.TryParse(vnpAmount, out var parsedAmount) ? parsedAmount / 100m : 0;
            decimal feePercentage = 5m;

            var success = await _registrationService.MarkAsPaidAsync(registrationId, tenantId, transactionNo ?? "", amount, feePercentage);
            if (success)
            {
                _logger.LogInformation($"VNPAY IPN processed success for Registration: {registrationId}.");
                
                // Trigger SignalR Notification
                TriggerNotification(reg.EventId, tenantId, registrationId);
                
                return Json(new { RspCode = "00", Message = "Confirm success" });
            }
            return Json(new { RspCode = "99", Message = "Update fail" });
        }

        await _registrationService.CancelAsync(registrationId, tenantId);
        return Json(new { RspCode = "00", Message = "Confirm success" });
    }

    private void TriggerNotification(string eventId, string tenantId, string registrationId)
    {
        _ = Task.Run(async () => 
        {
            try
            {
                var webApiUrl = _configuration["WebApiBaseUrl"] ?? "https://localhost:7296";
                var handler = new System.Net.Http.HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                };
                using var client = new System.Net.Http.HttpClient(handler);
                client.DefaultRequestHeaders.Add("X-API-KEY", "Secret_EMS_Api_Key_2026");
                var response = await client.PostAsync($"{webApiUrl}/api/notifications/trigger-registration?eventId={eventId}&tenantId={tenantId}&registrationId={registrationId}", null);
                _logger.LogInformation($"SignalR Trigger Response (Payment): {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger SignalR notification after payment.");
            }
        });
    }
}
