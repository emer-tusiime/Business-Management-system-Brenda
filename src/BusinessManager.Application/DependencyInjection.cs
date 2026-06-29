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
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<ISaleService, SaleService>();
        services.AddSingleton<IExpenseService, ExpenseService>();
        services.AddSingleton<IDebtorService, DebtorService>();
        services.AddSingleton<ISavingService, SavingService>();
        services.AddSingleton<IClientOrderService, ClientOrderService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IServiceItemService, ServiceItemService>();
        services.AddSingleton<DbAccessGate>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<ISettingService, SettingService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // Validators
        services.AddSingleton<IValidator<User>, UserValidator>();
        services.AddSingleton<IValidator<Sale>, SaleValidator>();
        services.AddSingleton<IValidator<Expense>, ExpenseValidator>();
        services.AddSingleton<IValidator<Product>, ProductValidator>();

        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        return services;
    }
}
