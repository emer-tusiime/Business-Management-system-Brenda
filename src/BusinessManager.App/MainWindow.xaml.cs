using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using BusinessManager.App.Services;
using BusinessManager.App.ViewModels;
using BusinessManager.Domain.Entities;
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
        Hide(); // keep main window hidden until login is complete
        await ShowLoginAsync();
    }

    private async Task ShowLoginAsync()
    {
        var loginWindow = _serviceProvider.GetRequiredService<Views.Auth.LoginWindow>();
        if (loginWindow.ShowDialog() != true)
        {
            System.Windows.Application.Current.Shutdown();
            return;
        }

        _currentUser = loginWindow.CurrentUser;
        UpdateUIForCurrentUser();
        NavigateToDashboard(null, null);
        Show(); // reveal main window only after successful login
    }

    public MaterialDesignThemes.Wpf.SnackbarMessageQueue SnackbarQueue =>
        (MaterialDesignThemes.Wpf.SnackbarMessageQueue)MainSnackbar.MessageQueue;

    private void UpdateUIForCurrentUser()
    {
        if (_currentUser != null)
            CurrentUserText.Text = _currentUser.FullName;
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

    internal void NavigateToDashboard(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Dashboard.DashboardView>();
    }

    internal void NavigateToSales(object? sender, RoutedEventArgs? e)
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

    internal void NavigateToExpenses(object? sender, RoutedEventArgs? e)
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

    internal void NavigateToDebtors(object? sender, RoutedEventArgs? e)
    {
        var debtorsViewModel = new ViewModels.DebtorsViewModel(
            _serviceProvider.GetRequiredService<IDebtorService>(),
            _serviceProvider.GetRequiredService<INotificationService>(),
            _serviceProvider.GetRequiredService<IDialogService>(),
            _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BusinessManager.App.ViewModels.DebtorsViewModel>>(),
            _currentUser!
        );

        MainContent.Content = new Views.Debtors.DebtorsView
        {
            DataContext = debtorsViewModel
        };
    }

    internal void NavigateToInventory(object? sender, RoutedEventArgs? e)
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

    internal void NavigateToReports(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Reports.ReportsView>();
    }

    private void NavigateToSettings(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Settings.SettingsView>();
    }

    private void NavigateToBackup(object? sender, RoutedEventArgs? e)
    {
        _navigationService.NavigateTo<Views.Backup.BackupView>();
    }
}
