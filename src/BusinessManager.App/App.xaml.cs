using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using BusinessManager.Infrastructure;
using BusinessManager.Application.Services;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Infrastructure.Data;

namespace BusinessManager.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Configure Serilog
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            // Create host
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.AddInfrastructure(context.Configuration);
                    BusinessManager.Application.DependencyInjection.AddApplication(services);
                    services.AddPresentation();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            // Initialize database
            await InitializeDatabaseAsync();

            WireNotificationService(_host.Services);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            await context.Database.EnsureCreatedAsync();
            
            await SeedDataAsync(scope.ServiceProvider);
            await EnsureBusinessDataAsync(scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Database initialization failed");
            throw;
        }
    }

    private async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>();

            // Check if data already exists
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Database already contains data, skipping seed");
                return;
            }

            logger.LogInformation("Seeding initial data...");
            
            // Seed data is already configured in the DbContext
            await context.SaveChangesAsync();
            
            logger.LogInformation("Initial data seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Data seeding failed");
            throw;
        }
    }

    private async Task EnsureBusinessDataAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>();

        var requiredServices = new[]
        {
            new BusinessManager.Domain.Entities.ServiceItem { Name = "Scanning", Category = BusinessManager.Domain.Enums.ServiceCategory.Scanning, DefaultPrice = 500, IsFlexiblePrice = true, IsActive = true },
            new BusinessManager.Domain.Entities.ServiceItem { Name = "Laminating", Category = BusinessManager.Domain.Enums.ServiceCategory.Laminating, DefaultPrice = 1000, IsFlexiblePrice = true, IsActive = true }
        };

        foreach (var service in requiredServices)
        {
            if (!await context.ServiceItems.AnyAsync(s => s.Name == service.Name))
            {
                context.ServiceItems.Add(service);
                logger.LogInformation("Added missing service: {ServiceName}", service.Name);
            }
        }

        var businessName = await context.Settings.FirstOrDefaultAsync(s => s.Key == "BusinessName");
        if (businessName != null && businessName.Value != "Alinda Brenda")
        {
            businessName.Value = "Alinda Brenda";
        }

        if (!await context.Settings.AnyAsync(s => s.Key == "DrawerOpeningBalance"))
        {
            context.Settings.Add(new BusinessManager.Domain.Entities.Setting
            {
                Key = "DrawerOpeningBalance",
                Value = "0",
                Description = "Cash in drawer at start of the day",
                Category = "Financial"
            });
        }

        var adminId = await context.Users
            .AsNoTracking()
            .Where(u => u.Username == "admin")
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (adminId != 0)
        {
            var authService = serviceProvider.GetRequiredService<IAuthService>();
            await authService.SetPasswordAsync(adminId, "Admin123");
            logger.LogInformation("Ensured default admin credentials are valid");
        }

        await context.SaveChangesAsync();
    }

    private void WireNotificationService(IServiceProvider services)
    {
        var notificationService = services.GetRequiredService<INotificationService>();
        if (notificationService is NotificationService uiNotifications)
        {
            uiNotifications.ConfirmationRequested += message =>
                MessageBox.Show(message, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            uiNotifications.SuccessMessage += message =>
                MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            uiNotifications.ErrorMessage += message =>
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            uiNotifications.WarningMessage += message =>
                MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
