using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IAuthService authService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Username = request.Username,
            FullName = request.FullName,
            Email = request.Email,
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        return await _authService.RegisterAsync(user, request.Password);
    }

    public async Task<User> UpdateUserAsync(UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.Id)
            ?? throw new InvalidOperationException("User not found");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        return await _userRepository.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        return await _userRepository.DeleteAsync(userId);
    }

    public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        return await _authService.SetPasswordAsync(userId, newPassword);
    }
}
