using System;
using System.Collections.Generic;

namespace EMS.BlazorWASM.Services;

public class ToastService
{
    public event Action<ToastModel>? OnShow;

    public void ShowSuccess(string message) => OnShow?.Invoke(new ToastModel { Message = message, Type = ToastType.Success });
    public void ShowError(string message) => OnShow?.Invoke(new ToastModel { Message = message, Type = ToastType.Error });
    public void ShowWarning(string message) => OnShow?.Invoke(new ToastModel { Message = message, Type = ToastType.Warning });
    public void ShowInfo(string message) => OnShow?.Invoke(new ToastModel { Message = message, Type = ToastType.Info });

    // Registration Notification Logic
    private Dictionary<string, RegistrationNotificationState> _registrationStates = new();
    private HashSet<string> _processedRegistrations = new();

    public void ReceiveRegistrationNotification(string eventTitle, int count, string registrationId)
    {
        if (!string.IsNullOrEmpty(registrationId))
        {
            if (_processedRegistrations.Contains(registrationId)) return;
            _processedRegistrations.Add(registrationId);
        }
        if (!_registrationStates.TryGetValue(eventTitle, out var state))
        {
            state = new RegistrationNotificationState();
            _registrationStates[eventTitle] = state;
        }

        state.Total += count;

        if (!state.IsShowing)
        {
            ShowRegistrationToast(eventTitle, state);
        }
    }

    private void ShowRegistrationToast(string eventTitle, RegistrationNotificationState state)
    {
        state.IsShowing = true;
        state.LastShown = state.Total;

        OnShow?.Invoke(new ToastModel
        {
            Message = $"Sự kiện {eventTitle} vừa có thêm {state.LastShown} người đăng ký mới!",
            Type = ToastType.Registration,
            EventTitle = eventTitle
        });
    }

    public void HandleToastClosed(string eventTitle)
    {
        if (_registrationStates.TryGetValue(eventTitle, out var state))
        {
            state.IsShowing = false;
            if (state.Total > state.LastShown)
            {
                ShowRegistrationToast(eventTitle, state);
            }
        }
    }

    public void HandleToastChecked(string eventTitle)
    {
        if (_registrationStates.TryGetValue(eventTitle, out var state))
        {
            state.IsShowing = false;
            state.Total = 0;
            state.LastShown = 0;
        }
    }

    // CheckIn Notification Logic
    public event Action<string>? OnCheckInReceived;
    public void ReceiveCheckInNotification(string eventId)
    {
        OnCheckInReceived?.Invoke(eventId);
    }
}

public class RegistrationNotificationState
{
    public int Total { get; set; }
    public int LastShown { get; set; }
    public bool IsShowing { get; set; }
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info,
    Registration
}

public class ToastModel
{
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; }
    public string EventTitle { get; set; } = string.Empty;
}
