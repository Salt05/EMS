using System;

namespace EMS.BlazorWASM.Services;

public class ToastService
{
    public event Action<string, ToastType>? OnShow;

    public void ShowSuccess(string message) => OnShow?.Invoke(message, ToastType.Success);
    public void ShowError(string message) => OnShow?.Invoke(message, ToastType.Error);
    public void ShowWarning(string message) => OnShow?.Invoke(message, ToastType.Warning);
    public void ShowInfo(string message) => OnShow?.Invoke(message, ToastType.Info);
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}
