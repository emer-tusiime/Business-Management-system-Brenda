using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void ShowInfo(string message)
    {
        _logger.LogInformation("Info: {Message}", message);
        // This will be implemented in the UI layer to show actual notifications
        InfoMessage?.Invoke(message);
    }

    public void ShowSuccess(string message)
    {
        _logger.LogInformation("Success: {Message}", message);
        SuccessMessage?.Invoke(message);
    }

    public void ShowWarning(string message)
    {
        _logger.LogWarning("Warning: {Message}", message);
        WarningMessage?.Invoke(message);
    }

    public void ShowError(string message)
    {
        _logger.LogError("Error: {Message}", message);
        ErrorMessage?.Invoke(message);
    }

    public async Task<bool> ShowConfirmationAsync(string message)
    {
        return await Task.FromResult(ShowConfirmation(message));
    }

    public bool ShowConfirmation(string message)
    {
        _logger.LogInformation("Confirmation requested: {Message}", message);
        return ConfirmationRequested?.Invoke(message) ?? false;
    }

    // Events that the UI can subscribe to
    public event Action<string>? InfoMessage;
    public event Action<string>? SuccessMessage;
    public event Action<string>? WarningMessage;
    public event Action<string>? ErrorMessage;
    public event Func<string, bool>? ConfirmationRequested;
}
