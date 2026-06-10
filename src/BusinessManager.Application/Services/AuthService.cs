using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<(User? user, string? errorMessage)> LoginAsync(string username, string password)
    {
        try
        {
            var normalizedUsername = username.Trim();
            var normalizedPassword = password.Trim();

            var user = await _userRepository.GetByUsernameAsync(normalizedUsername);
            if (user == null)
            {
                return (null, "Invalid username or password");
            }

            if (!user.IsActive)
            {
                return (null, "Account is deactivated");
            }

            if (!VerifyPassword(normalizedPassword, user.PasswordHash))
            {
                return (null, "Invalid username or password");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {Username} logged in successfully", username);
            return (user, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", username);
            return (null, "An error occurred during login");
        }
    }

    public async Task<User> RegisterAsync(User user, string password)
    {
        try
        {
            if (await UserExistsAsync(user.Username))
            {
                throw new InvalidOperationException("Username already exists");
            }

            if (!string.IsNullOrEmpty(user.Email) && await EmailExistsAsync(user.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            user.PasswordHash = HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            var createdUser = await _userRepository.AddAsync(user);
            _logger.LogInformation("User {Username} registered successfully", user.Username);

            return createdUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username}", user.Username);
            throw;
        }
    }

    public async Task<bool> SetPasswordAsync(int userId, string newPassword)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Password set successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (!VerifyPassword(currentPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        try
        {
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(string username)
    {
        return await _userRepository.GetByUsernameAsync(username) != null;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email) != null;
    }

    private static string HashPassword(string password) => PasswordHasher.Hash(password);

    private static bool VerifyPassword(string password, string hash) => PasswordHasher.Verify(password, hash);
}
