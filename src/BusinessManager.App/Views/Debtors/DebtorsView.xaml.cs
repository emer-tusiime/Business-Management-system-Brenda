using System.Windows.Controls;

namespace BusinessManager.App.Views.Debtors;

public partial class DebtorsView : UserControl
{
    public DebtorsView()
    {
        InitializeComponent();
    }

    private async void DebtorsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.DebtorsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
