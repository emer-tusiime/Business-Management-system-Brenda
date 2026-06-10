using Microsoft.Extensions.DependencyInjection;
using BusinessManager.Application.Services;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Domain.Entities;
using BusinessManager.Application.Validators;
using FluentValidation;
using AutoMapper;

namespace BusinessManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // Validators
        services.AddScoped<IValidator<User>, UserValidator>();
        services.AddScoped<IValidator<Sale>, SaleValidator>();
        services.AddScoped<IValidator<Expense>, ExpenseValidator>();
        services.AddScoped<IValidator<Product>, ProductValidator>();

        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        return services;
    }
}
