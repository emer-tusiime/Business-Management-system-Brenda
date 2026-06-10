using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Settings;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void SettingsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
