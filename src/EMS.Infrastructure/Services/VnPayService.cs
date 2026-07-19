using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using EMS.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EMS.Infrastructure.Services;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;

    public VnPayService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreatePaymentUrl(string ipAddress, string registrationId, decimal amount, string eventTitle, string returnUrl)
    {
        var vnpayUrl = _configuration["Vnpay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var tmnCode = GetRequiredSetting("Vnpay:TmnCode");
        var hashSecret = GetRequiredSetting("Vnpay:HashSecret");

        var vnpData = new SortedDictionary<string, string>(StringComparer.Ordinal);
        
        vnpData.Add("vnp_Version", "2.1.0");
        vnpData.Add("vnp_Command", "pay");
        vnpData.Add("vnp_TmnCode", tmnCode);
        
        // VNPAY amount is in VND, multiplied by 100 as per spec
        var rawAmount = ((long)(amount * 100)).ToString();
        vnpData.Add("vnp_Amount", rawAmount);
        
        vnpData.Add("vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss")); // GMT+7
        vnpData.Add("vnp_CurrCode", "VND");
        if (string.IsNullOrEmpty(ipAddress) || ipAddress.Contains(":") || ipAddress.Length > 15) 
        {
            ipAddress = "127.0.0.1";
        }
        vnpData.Add("vnp_IpAddr", ipAddress);
        vnpData.Add("vnp_Locale", "vn");
        
        var cleanTitle = RemoveVietnameseSign(eventTitle).Replace(" ", "");
        var orderInfo = $"ThanhToanVe_{cleanTitle}";
        if (orderInfo.Length > 255) orderInfo = orderInfo.Substring(0, 255);
        vnpData.Add("vnp_OrderInfo", orderInfo);
        vnpData.Add("vnp_OrderType", "250000"); // 250000 - thanh toan ve su kien
        vnpData.Add("vnp_ReturnUrl", returnUrl);
        
        // Unique transaction ref - we combine RegistrationId with a timestamp suffix to allow retries
        vnpData.Add("vnp_TxnRef", $"{registrationId}-{DateTime.UtcNow.Ticks}");

        var queryBuilder = new StringBuilder();
        var dataBuilder = new StringBuilder();

        foreach (var kvp in vnpData)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                queryBuilder.Append(WebUtility.UrlEncode(kvp.Key));
                queryBuilder.Append("=");
                queryBuilder.Append(WebUtility.UrlEncode(kvp.Value));
                queryBuilder.Append("&");

                dataBuilder.Append(WebUtility.UrlEncode(kvp.Key));
                dataBuilder.Append("=");
                dataBuilder.Append(WebUtility.UrlEncode(kvp.Value));
                dataBuilder.Append("&");
            }
        }

        var queryString = queryBuilder.ToString().TrimEnd('&');
        var rawData = dataBuilder.ToString().TrimEnd('&');

        var secureHash = HmacSha512(hashSecret, rawData);
        
        var paymentUrl = $"{vnpayUrl}?{queryString}&vnp_SecureHash={secureHash}";
        return paymentUrl;
    }

    public bool ValidateCallback(Dictionary<string, string> queryParameters)
    {
        var hashSecret = GetRequiredSetting("Vnpay:HashSecret");
        
        if (!queryParameters.TryGetValue("vnp_SecureHash", out var secureHash))
        {
            return false;
        }

        var vnpData = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kvp in queryParameters)
        {
            if (kvp.Key.StartsWith("vnp_") && kvp.Key != "vnp_SecureHash" && kvp.Key != "vnp_SecureHashType")
            {
                vnpData.Add(kvp.Key, kvp.Value);
            }
        }

        var dataBuilder = new StringBuilder();
        foreach (var kvp in vnpData)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                dataBuilder.Append(WebUtility.UrlEncode(kvp.Key));
                dataBuilder.Append("=");
                dataBuilder.Append(WebUtility.UrlEncode(kvp.Value));
                dataBuilder.Append("&");
            }
        }

        var rawData = dataBuilder.ToString().TrimEnd('&');
        var calculatedHash = HmacSha512(hashSecret, rawData);

        return calculatedHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            var hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }

    private string GetRequiredSetting(string key)
    {
        var value = _configuration[key] ?? Environment.GetEnvironmentVariable(key.Replace(':', '_'));
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing or empty required payment setting: {key}. Vui lòng cấu hình VNPay TmnCode và HashSecret.");
        }
        return value;
    }

    private static string RemoveVietnameseSign(string str)
    {
        string[] signs = new string[]
        {
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        };

        for (int i = 1; i < signs.Length; i++)
        {
            for (int j = 0; j < signs[i].Length; j++)
            {
                str = str.Replace(signs[i][j].ToString(), signs[0][i - 1].ToString());
            }
        }
        
        // Remove special chars to avoid URL errors
        var sb = new StringBuilder();
        foreach (var c in str)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == ' ' || c == '-' || c == '_')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
