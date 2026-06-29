using System;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using BusinessManager.App.ViewModels;
using BusinessManager.Domain.Entities;

namespace BusinessManager.App.Views.Auth;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public User? CurrentUser { get; private set; }

    public LoginWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        
        _viewModel = serviceProvider.GetRequiredService<LoginViewModel>();
        _viewModel.LoginSuccess += OnLoginSuccess;
        _viewModel.LoginFailed += OnLoginFailed;
        
        DataContext = _viewModel;

        PasswordBox.PasswordChanged += (_, _) => _viewModel.Password = PasswordBox.Password;
        
        Loaded += (s, e) => UsernameTextBox.Focus();
    }

    private void OnLoginSuccess(User user)
    {
        CurrentUser = user;
        DialogResult = true;
        Close();
    }

    private void OnLoginFailed(string errorMessage)
    {
        // ErrorMessage binding on the ViewModel drives visibility via BooleanToVisibilityConverter
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;
        _viewModel.LoginCommand.Execute(null);
    }

    private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            LoginButton_Click(sender, e);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LoginSuccess -= OnLoginSuccess;
        _viewModel.LoginFailed -= OnLoginFailed;
        base.OnClosed(e);
    }
}
