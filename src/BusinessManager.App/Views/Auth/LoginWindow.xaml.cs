using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
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
        _viewModel.LoginFailed  += OnLoginFailed;

        DataContext = _viewModel;

        PasswordBox.PasswordChanged += (_, _) => _viewModel.Password = PasswordBox.Password;
        Loaded += (_, _) =>
        {
            LoadCompanyIcon();
            UsernameTextBox.Focus();
        };
    }

    private void LoadCompanyIcon()
    {
        try
        {
            var exeDir   = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                           ?? AppDomain.CurrentDomain.BaseDirectory;
            var iconPath = Path.Combine(exeDir, "Assets", "icon.png");

            if (!File.Exists(iconPath)) return;

            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource        = new Uri(iconPath, UriKind.Absolute);
            bi.CacheOption      = BitmapCacheOption.OnLoad;
            bi.DecodePixelWidth = 116;
            bi.EndInit();

            CompanyLogo.Source      = bi;
            CompanyLogo.Visibility  = Visibility.Visible;
            FallbackIcon.Visibility = Visibility.Collapsed;
        }
        catch
        {
            // Keep fallback PackIcon if image fails to load
        }
    }

    private void OnLoginSuccess(User user)
    {
        CurrentUser  = user;
        DialogResult = true;
        Close();
    }

    private void OnLoginFailed(string _) { }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;
        _viewModel.LoginCommand.Execute(null);
    }

    private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            LoginButton_Click(sender, e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LoginSuccess -= OnLoginSuccess;
        _viewModel.LoginFailed  -= OnLoginFailed;
        base.OnClosed(e);
    }
}
