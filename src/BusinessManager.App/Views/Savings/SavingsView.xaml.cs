using System.Windows.Controls;

namespace BusinessManager.App.Views.Savings;

public partial class SavingsView : UserControl
{
    public SavingsView()
    {
        InitializeComponent();
    }

    private async void SavingsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SavingsViewModel vm)
            await vm.InitializeAsync();
    }
}
