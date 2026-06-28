using System.Windows;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += DashboardView_Loaded;
    }

    private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.DashboardViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }

    private void QuickAction_NewSale(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
            main.NavigateToSales(sender, e);
    }

    private void QuickAction_AddExpense(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
            main.NavigateToExpenses(sender, e);
    }

    private void QuickAction_Reports(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
            main.NavigateToReports(sender, e);
    }

    private void QuickAction_Inventory(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
            main.NavigateToInventory(sender, e);
    }
}
