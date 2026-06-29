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

            // Enable WAL mode and tune sync for better read/write performance
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");

            await EnsureDebtorsSchemaAsync(context);
            await EnsureNewTablesAsync(context);
            await SeedDataAsync(scope.ServiceProvider);

            _ = Task.Run(async () =>
            {
                try
                {
                    using var bgScope = _host!.Services.CreateScope();
                    await EnsureBusinessDataAsync(bgScope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning(ex, "Background business data sync failed");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Database initialization failed");
            throw;
        }
    }

    private async Task EnsureDebtorsSchemaAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Debtors" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "CustomerName" TEXT NOT NULL,
                "Phone" TEXT NULL,
                "Description" TEXT NOT NULL,
                "TotalAmount" REAL NOT NULL,
                "AmountPaid" REAL NOT NULL,
                "RecordDate" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "UserId" INTEGER NOT NULL,
                "IsSettled" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NULL,
                FOREIGN KEY("UserId") REFERENCES "Users"("Id")
            );
            """);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "DebtPayments" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "DebtorId" INTEGER NOT NULL,
                "Amount" REAL NOT NULL,
                "PaymentDate" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "UserId" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                FOREIGN KEY("DebtorId") REFERENCES "Debtors"("Id") ON DELETE CASCADE,
                FOREIGN KEY("UserId") REFERENCES "Users"("Id")
            );
            """);
    }

    private async Task EnsureNewTablesAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Savings" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "Date" TEXT NOT NULL,
                "Amount" REAL NOT NULL,
                "Notes" TEXT NULL,
                "UserId" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                FOREIGN KEY("UserId") REFERENCES "Users"("Id")
            );
            """);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ClientOrders" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "ClientName" TEXT NOT NULL,
                "Phone" TEXT NULL,
                "Description" TEXT NOT NULL,
                "OrderDate" TEXT NOT NULL,
                "PickupDate" TEXT NOT NULL,
                "Status" INTEGER NOT NULL DEFAULT 1,
                "Notes" TEXT NULL,
                "UserId" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NULL,
                FOREIGN KEY("UserId") REFERENCES "Users"("Id")
            );
            """);
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

        var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (admin != null && !PasswordHasher.Verify("Admin123", admin.PasswordHash))
        {
            var authService = serviceProvider.GetRequiredService<IAuthService>();
            await authService.SetPasswordAsync(admin.Id, "Admin123");
            logger.LogInformation("Reset admin password to default");
        }

        var expensesNeedingDate = await context.Expenses
            .Where(e => e.ExpenseDate < new DateTime(2000, 1, 1))
            .ToListAsync();
        foreach (var expense in expensesNeedingDate)
        {
            expense.ExpenseDate = expense.CreatedAt;
        }

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Debtors SET IsSettled = 0 WHERE TotalAmount > AmountPaid");
        }
        catch
        {
            // Debtors table may not exist yet on first run
        }

        await context.SaveChangesAsync();
    }

    private void WireNotificationService(IServiceProvider services)
    {
        var notificationService = services.GetRequiredService<INotificationService>();
        var mainWindow = services.GetRequiredService<MainWindow>();

        if (notificationService is NotificationService uiNotifications)
        {
            // Confirmations stay as modal dialogs — they need a yes/no answer
            uiNotifications.ConfirmationRequested += message =>
                MessageBox.Show(message, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

            // All other notifications use the non-blocking Snackbar toast
            uiNotifications.SuccessMessage += message =>
                mainWindow.SnackbarQueue.Enqueue(message);
            uiNotifications.ErrorMessage += message =>
                mainWindow.SnackbarQueue.Enqueue("Error: " + message);
            uiNotifications.WarningMessage += message =>
                mainWindow.SnackbarQueue.Enqueue(message);
            uiNotifications.InfoMessage += message =>
                mainWindow.SnackbarQueue.Enqueue(message);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
