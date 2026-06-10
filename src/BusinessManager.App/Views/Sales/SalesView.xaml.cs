using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Sales;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
    }

    private async void SalesView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SalesViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
