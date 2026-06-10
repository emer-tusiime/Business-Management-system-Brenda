using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace BusinessManager.App.Services;

public interface INavigationService
{
    void NavigateTo<T>() where T : class;
    void NavigateTo<T>(object parameter) where T : class;
    void GoBack();
    bool CanGoBack { get; }
}

public interface IDialogService
{
    void ShowMessage(string message, string title = "Information");
    bool ShowConfirmation(string message, string title = "Confirmation");
    string? ShowInputDialog(string message, string title = "Input", string defaultValue = "");
    T? ShowDialog<T>() where T : class, new();
}

public class NavigationService : INavigationService
{
    private readonly Stack<FrameworkElement> _navigationStack = new();
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private MainWindow MainWindow => _serviceProvider.GetRequiredService<MainWindow>();

    public bool CanGoBack => _navigationStack.Count > 1;

    public void NavigateTo<T>() where T : class
    {
        NavigateTo<T>(null);
    }

    public void NavigateTo<T>(object? parameter) where T : class
    {
        var page = _serviceProvider.GetRequiredService<T>();
        
        if (page is FrameworkElement frameworkElement)
        {
            // Set DataContext if it's a View with ViewModel
            if (frameworkElement.DataContext == null)
            {
                var viewModelType = Type.GetType($"BusinessManager.App.ViewModels.{typeof(T).Name.Replace("Window", "ViewModel")}");
                if (viewModelType != null)
                {
                    var viewModel = _serviceProvider.GetRequiredService(viewModelType);
                    frameworkElement.DataContext = viewModel;
                }
            }

            // Handle parameter
            if (parameter != null && frameworkElement.DataContext is IParameterReceiver receiver)
            {
                receiver.ReceiveParameter(parameter);
            }

            _navigationStack.Push(frameworkElement);
            MainWindow.MainContent.Content = frameworkElement;
        }
    }

    public void GoBack()
    {
        if (!CanGoBack) return;

        _navigationStack.Pop(); // Remove current
        var previous = _navigationStack.Peek();
        MainWindow.MainContent.Content = previous;
    }
}

public interface IParameterReceiver
{
    void ReceiveParameter(object parameter);
}

public class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowMessage(string message, string title = "Information")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public bool ShowConfirmation(string message, string title = "Confirmation")
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public string? ShowInputDialog(string message, string title = "Input", string defaultValue = "")
    {
        var dialog = new InputDialog(message, title, defaultValue);
        if (dialog.ShowDialog() == true)
        {
            return dialog.InputText;
        }
        return null;
    }

    public T? ShowDialog<T>() where T : class, new()
    {
        var dialog = _serviceProvider.GetRequiredService<T>();
        if (dialog is Window window)
        {
            return window.ShowDialog() == true ? dialog : null;
        }
        return null;
    }
}
