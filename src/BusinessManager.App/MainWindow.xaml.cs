using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using BusinessManager.App.Services;
using BusinessManager.App.ViewModels;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Enums;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.App;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;
    private User? _currentUser;

    public MainWindow(IServiceProvider serviceProvider, INavigationService navigationService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;
        
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Show welcome screen first
        var welcomeWindow = _serviceProvider.GetRequiredService<Views.Welcome.WelcomeWindow>();
        var result = welcomeWindow.ShowDialog();

        if (result != true)
        {
            // User closed welcome window, exit application
            System.Windows.Application.Current.Shutdown();
            return;
        }

        // Show login screen
        await ShowLoginAsync();
    }

    private async Task ShowLoginAsync()
    {
        var loginWindow = _serviceProvider.GetRequiredService<Views.Auth.LoginWindow>();
        var result = loginWindow.ShowDialog();

        if (result != true)
        {
            // User cancelled login or closed window
            System.Windows.Application.Current.Shutdown();
            return;
        }

        // Get current user from login window
        _currentUser = loginWindow.CurrentUser;
        UpdateUIForCurrentUser();
        
        // Navigate to dashboard
        NavigateToDashboard(null, null);
    }

    private void UpdateUIForCurrentUser()
    {
        if (_currentUser != null)
        {
            CurrentUserText.Text = _currentUser.FullName;
            
            // Show users menu only for admin
            if (_currentUser.Role == UserRole.Admin)
            {
                UsersMenuButton.Visibility = Visibility.Visible;
            }
        }
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        // Toggle sidebar visibility if needed
        // For now, sidebar is always visible
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        if (dialogService.ShowConfirmation("Are you sure you want to logout?"))
        {
            _currentUser = null;
            await ShowLoginAsync();
        }
    }

    private void NavigateToDashboard(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Dashboard.DashboardView>();
    }

    private void NavigateToSales(object? sender, RoutedEventArgs? e)
    {
        // Create SalesViewModel with current user
        var salesViewModel = new ViewModels.SalesViewModel(
            _serviceProvider.GetRequiredService<ISaleService>(),
            _serviceProvider.GetRequiredService<IServiceItemRepository>(),
            _serviceProvider.GetRequiredService<IProductRepository>(),
            _serviceProvider.GetRequiredService<INotificationService>(),
            _serviceProvider.GetRequiredService<IDialogService>(),
            _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BusinessManager.App.ViewModels.SalesViewModel>>(),
            _currentUser!
        );

        var salesView = new Views.Sales.SalesView
        {
            DataContext = salesViewModel
        };

        MainContent.Content = salesView;
    }

    private void NavigateToExpenses(object? sender, RoutedEventArgs? e)
    {
        // Create ExpensesViewModel with current user
        var expensesViewModel = new ViewModels.ExpensesViewModel(
            _serviceProvider.GetRequiredService<IExpenseService>(),
            _serviceProvider.GetRequiredService<IExpenseCategoryRepository>(),
            _serviceProvider.GetRequiredService<INotificationService>(),
            _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BusinessManager.App.ViewModels.ExpensesViewModel>>(),
            _currentUser!
        );

        var expensesView = new Views.Expenses.ExpensesView
        {
            DataContext = expensesViewModel
        };

        MainContent.Content = expensesView;
    }

    private void NavigateToInventory(object? sender, RoutedEventArgs? e)
    {
        // Create InventoryViewModel with current user
        var inventoryViewModel = new ViewModels.InventoryViewModel(
            _serviceProvider.GetRequiredService<IInventoryService>(),
            _serviceProvider.GetRequiredService<IProductRepository>(),
            _serviceProvider.GetRequiredService<INotificationService>(),
            _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BusinessManager.App.ViewModels.InventoryViewModel>>(),
            _currentUser!
        );

        var inventoryView = new Views.Inventory.InventoryView
        {
            DataContext = inventoryViewModel
        };

        MainContent.Content = inventoryView;
    }

    private void NavigateToReports(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Reports.ReportsView>();
    }

    private void NavigateToSettings(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Settings.SettingsView>();
    }

    private void NavigateToUsers(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Users.UsersView>();
    }

    private void NavigateToBackup(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Backup.BackupView>();
    }
}
