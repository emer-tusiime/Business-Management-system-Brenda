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
    private readonly DbAccessGate _dbGate;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IAuthService authService,
        DbAccessGate dbGate,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _dbGate = dbGate;
        _logger = logger;
    }

    public Task<IEnumerable<User>> GetAllUsersAsync() =>
        _dbGate.RunAsync<IEnumerable<User>>(() => _userRepository.GetAllAsync());

    public Task<User?> GetUserByIdAsync(int id) =>
        _dbGate.RunAsync(() => _userRepository.GetByIdAsync(id));

    public Task<User> CreateUserAsync(CreateUserRequest request) =>
        _dbGate.RunAsync(async () =>
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
        });

    public Task<User> UpdateUserAsync(UpdateUserRequest request) =>
        _dbGate.RunAsync(async () =>
        {
            var user = await _userRepository.GetByIdAsync(request.Id)
                ?? throw new InvalidOperationException("User not found");

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Role = request.Role;
            user.IsActive = request.IsActive;

            return await _userRepository.UpdateAsync(user);
        });

    public Task<bool> DeleteUserAsync(int userId) =>
        _dbGate.RunAsync(() => _userRepository.DeleteAsync(userId));

    public Task<bool> ResetPasswordAsync(int userId, string newPassword) =>
        _dbGate.RunAsync(async () =>
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new InvalidOperationException("User not found");
            return await _authService.SetPasswordAsync(userId, newPassword);
        });
}
