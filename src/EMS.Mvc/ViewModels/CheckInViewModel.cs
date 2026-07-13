using EMS.Core.Entities;

namespace EMS.Mvc.ViewModels;

public class CheckInViewModel
{
    public Event Event { get; set; } = null!;

    /// <summary>Mã check-in sinh viên nhập vào</summary>
    public string CheckInCode { get; set; } = string.Empty;

    /// <summary>Kết quả check-in sau khi submit</summary>
    public bool? CheckInSuccess { get; set; }

    /// <summary>Thông điệp phản hồi check-in</summary>
    public string? CheckInMessage { get; set; }
}
