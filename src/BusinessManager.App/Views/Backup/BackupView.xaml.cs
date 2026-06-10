using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BusinessManager.App.Views.Backup;

public partial class BackupView : UserControl
{
    public BackupView()
    {
        InitializeComponent();
    }

    private async void BackupView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.BackupRestoreViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
