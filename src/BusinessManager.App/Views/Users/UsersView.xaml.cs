using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Users;

public partial class UsersView : UserControl
{
    public UsersView()
    {
        InitializeComponent();
    }

    private async void UsersView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.UsersViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
