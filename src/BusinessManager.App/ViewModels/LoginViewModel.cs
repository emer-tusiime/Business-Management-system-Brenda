using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System; using System.Collections.Generic; using System.Threading.Tasks; using CommunityToolkit.Mvvm.ComponentModel; using CommunityToolkit.Mvvm.Input; using Microsoft.Extensions.Logging; using BusinessManager.Domain.Interfaces; using BusinessManager.Domain.Entities; using BusinessManager.Domain.DTOs; using BusinessManager.Domain.Enums; using BusinessManager.Application.Services;
using BusinessManager.Domain.Entities;

namespace BusinessManager.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public event Action<User>? LoginSuccess;
    public event Action<string>? LoginFailed;

    public LoginViewModel(IAuthService authService, ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password";
                LoginFailed?.Invoke(ErrorMessage);
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            var (user, error) = await _authService.LoginAsync(Username, Password);

            if (user != null)
            {
                _logger.LogInformation("User {Username} logged in successfully", Username);
                LoginSuccess?.Invoke(user);
            }
            else
            {
                ErrorMessage = error ?? "Login failed";
                LoginFailed?.Invoke(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            ErrorMessage = "An error occurred during login";
            LoginFailed?.Invoke(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnUsernameChanged(string value)
    {
        ErrorMessage = string.Empty;
    }

    partial void OnPasswordChanged(string value)
    {
        ErrorMessage = string.Empty;
    }
}
