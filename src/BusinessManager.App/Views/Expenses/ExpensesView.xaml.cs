using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Expenses;

public partial class ExpensesView : UserControl
{
    public ExpensesView()
    {
        InitializeComponent();
    }

    private async void ExpensesView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ExpensesViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
