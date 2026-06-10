using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System; using System.Collections.Generic; using System.Threading.Tasks; using CommunityToolkit.Mvvm.ComponentModel; using CommunityToolkit.Mvvm.Input; using Microsoft.Extensions.Logging; using BusinessManager.Domain.Interfaces; using BusinessManager.Domain.Entities; using BusinessManager.Domain.DTOs; using BusinessManager.Domain.Enums; using BusinessManager.Application.Services;
using BusinessManager.Domain.Entities;

namespace BusinessManager.App.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UsersViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<User> _users = new();

    [ObservableProperty]
    private ObservableCollection<UserRole> _userRoles = new();

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private UserRole _selectedRole = UserRole.Attendant;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public UsersViewModel(
        IUserService userService,
        INotificationService notificationService,
        ILogger<UsersViewModel> logger)
    {
        _userService = userService;
        _notificationService = notificationService;
        _logger = logger;

        LoadUsersCommand = new RelayCommand(async () => await LoadUsersAsync());
        AddUserCommand = new RelayCommand(async () => await AddUserAsync(), CanAddUser);
        EditUserCommand = new RelayCommand<User>(EditUser);
        UpdateUserCommand = new RelayCommand(async () => await UpdateUserAsync(), CanUpdateUser);
        DeleteUserCommand = new RelayCommand<User>(async (user) => await DeleteUserAsync(user));
        ToggleUserStatusCommand = new RelayCommand<User>(async (user) => await ToggleUserStatusAsync(user));
        ResetPasswordCommand = new RelayCommand<User>(async (user) => await ResetPasswordAsync(user));
        ClearFormCommand = new RelayCommand(ClearForm);
        SearchCommand = new RelayCommand(async () => await FilterUsersAsync());
        RefreshCommand = new RelayCommand(async () => await LoadUsersAsync());
    }

    public IRelayCommand LoadUsersCommand { get; }
    public IRelayCommand AddUserCommand { get; }
    public IRelayCommand<User> EditUserCommand { get; }
    public IRelayCommand UpdateUserCommand { get; }
    public IRelayCommand<User> DeleteUserCommand { get; }
    public IRelayCommand<User> ToggleUserStatusCommand { get; }
    public IRelayCommand<User> ResetPasswordCommand { get; }
    public IRelayCommand ClearFormCommand { get; }
    public IRelayCommand SearchCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadUsersAsync();
        LoadUserRoles();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            var users = await _userService.GetAllUsersAsync();
            Users = new ObservableCollection<User>(users.OrderByDescending(u => u.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            _notificationService.ShowError("Error loading users");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadUserRoles()
    {
        UserRoles = new ObservableCollection<UserRole>
        {
            UserRole.Admin,
            UserRole.Attendant
        };
    }

    private bool CanAddUser()
    {
        return !string.IsNullOrWhiteSpace(Username) && 
               !string.IsNullOrWhiteSpace(FullName) && 
               !string.IsNullOrWhiteSpace(Email) && 
               !string.IsNullOrWhiteSpace(Password) &&
               Email.Contains("@");
    }

    private bool CanUpdateUser()
    {
        return SelectedUser != null && 
               !string.IsNullOrWhiteSpace(FullName) && 
               !string.IsNullOrWhiteSpace(Email) &&
               Email.Contains("@");
    }

    private async Task AddUserAsync()
    {
        try
        {
            if (!CanAddUser()) return;

            var createUserRequest = new CreateUserRequest
            {
                Username = Username,
                FullName = FullName,
                Email = Email,
                Password = Password,
                Role = SelectedRole,
                IsActive = IsActive
            };

            await _userService.CreateUserAsync(createUserRequest);
            _notificationService.ShowSuccess("User created successfully");
            ClearForm();
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            _notificationService.ShowError("Error creating user");
        }
    }

    private void EditUser(User? user)
    {
        if (user == null) return;

        SelectedUser = user;
        Username = user.Username;
        FullName = user.FullName;
        Email = user.Email;
        SelectedRole = user.Role;
        IsActive = user.IsActive;
        Password = string.Empty; // Don't populate password for security
        IsEditing = true;
    }

    private async Task UpdateUserAsync()
    {
        try
        {
            if (!CanUpdateUser()) return;

            var updateUserRequest = new UpdateUserRequest
            {
                Id = SelectedUser!.Id,
                FullName = FullName,
                Email = Email,
                Role = SelectedRole,
                IsActive = IsActive
            };

            await _userService.UpdateUserAsync(updateUserRequest);
            _notificationService.ShowSuccess("User updated successfully");
            ClearForm();
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            _notificationService.ShowError("Error updating user");
        }
    }

    private async Task DeleteUserAsync(User? user)
    {
        try
        {
            if (user == null) return;

            if (!_notificationService.ShowConfirmation($"Are you sure you want to delete user: {user.FullName}?"))
                return;

            await _userService.DeleteUserAsync(user.Id);
            _notificationService.ShowSuccess("User deleted successfully");
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            _notificationService.ShowError("Error deleting user");
        }
    }

    private async Task ToggleUserStatusAsync(User? user)
    {
        try
        {
            if (user == null) return;

            var updateUserRequest = new UpdateUserRequest
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsActive = !user.IsActive
            };

            await _userService.UpdateUserAsync(updateUserRequest);
            _notificationService.ShowSuccess($"User {(user.IsActive ? "deactivated" : "activated")} successfully");
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status");
            _notificationService.ShowError("Error toggling user status");
        }
    }

    private async Task ResetPasswordAsync(User? user)
    {
        try
        {
            if (user == null) return;

            if (!_notificationService.ShowConfirmation($"Are you sure you want to reset password for: {user.FullName}?"))
                return;

            var newPassword = "Temp123!"; // Generate random password in real implementation
            await _userService.ResetPasswordAsync(user.Id, newPassword);
            _notificationService.ShowSuccess($"Password reset successfully. New password: {newPassword}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            _notificationService.ShowError("Error resetting password");
        }
    }

    private void ClearForm()
    {
        Username = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
        Password = string.Empty;
        SelectedRole = UserRole.Attendant;
        IsActive = true;
        IsEditing = false;
        SelectedUser = null;
    }

    private async Task FilterUsersAsync()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            
            if (!string.IsNullOrEmpty(SearchText))
            {
                users = users.Where(u => 
                    u.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Users = new ObservableCollection<User>(users.OrderByDescending(u => u.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering users");
            _notificationService.ShowError("Error filtering users");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search
        Task.Delay(300).ContinueWith(_ => FilterUsersAsync());
    }
}
