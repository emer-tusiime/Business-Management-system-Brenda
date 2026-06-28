using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using BusinessManager.Infrastructure.Data;
using BusinessManager.Infrastructure.Repositories;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("server=", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = DatabasePathHelper.GetConnectionString();
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString), ServiceLifetime.Singleton, ServiceLifetime.Singleton);

        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IServiceItemRepository, ServiceItemRepository>();
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<ISaleRepository, SaleRepository>();
        services.AddSingleton<ISaleItemRepository, SaleItemRepository>();
        services.AddSingleton<IExpenseRepository, ExpenseRepository>();
        services.AddSingleton<IDebtorRepository, DebtorRepository>();
        services.AddSingleton<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddSingleton<IInventoryMovementRepository, InventoryMovementRepository>();
        services.AddSingleton<ISettingRepository, SettingRepository>();
        services.AddSingleton<IPriceHistoryRepository, PriceHistoryRepository>();
        services.AddSingleton<IAuditLogRepository, AuditLogRepository>();

        return services;
    }
}
