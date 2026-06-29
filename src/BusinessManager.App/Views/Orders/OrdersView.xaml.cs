using System.Windows.Controls;

namespace BusinessManager.App.Views.Orders;

public partial class OrdersView : UserControl
{
    public OrdersView()
    {
        InitializeComponent();
    }

    private async void OrdersView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.OrdersViewModel vm)
            await vm.InitializeAsync();
    }
}
