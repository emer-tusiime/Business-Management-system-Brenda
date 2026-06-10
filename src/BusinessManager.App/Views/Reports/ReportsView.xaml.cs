using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Reports;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    private async void ReportsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ReportsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
