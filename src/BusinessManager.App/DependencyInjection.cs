using Microsoft.Extensions.DependencyInjection;
using BusinessManager.App.Services;
using BusinessManager.App.ViewModels;
using BusinessManager.Application.Services;
using BusinessManager.Reporting;
using BusinessManager.App.Views.Auth;
using BusinessManager.App.Views.Backup;
using BusinessManager.App.Views.Dashboard;
using BusinessManager.App.Views.Expenses;
using BusinessManager.App.Views.Inventory;
using BusinessManager.App.Views.Orders;
using BusinessManager.App.Views.Reports;
using BusinessManager.App.Views.Sales;
using BusinessManager.App.Views.Savings;
using BusinessManager.App.Views.Settings;

namespace BusinessManager.App;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<IPdfGenerator, QuestPdfGenerator>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<NotificationCenter>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<LoginViewModel>();

        services.AddTransient<DashboardView>();
        services.AddSingleton<DashboardViewModel>(sp => new DashboardViewModel(
            sp.GetRequiredService<BusinessManager.Domain.Interfaces.IReportService>(),
            sp.GetRequiredService<BusinessManager.Domain.Interfaces.IInventoryService>(),
            sp.GetRequiredService<BusinessManager.Domain.Interfaces.IDebtorService>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DashboardViewModel>>()));

        services.AddTransient<SalesView>();
        services.AddTransient<ExpensesView>();
        services.AddTransient<InventoryView>();
        services.AddTransient<ReportsView>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsView>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BackupView>();
        services.AddTransient<BackupRestoreViewModel>();
        services.AddTransient<SavingsView>();
        services.AddTransient<OrdersView>();

        return services;
    }
}
