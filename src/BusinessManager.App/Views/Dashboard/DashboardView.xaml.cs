using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += DashboardView_Loaded;
    }

    private async void DashboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.DashboardViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }
}
