using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Inventory;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
    }

    private async void InventoryView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.InventoryViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
