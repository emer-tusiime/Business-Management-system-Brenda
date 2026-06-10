using System.Windows;
using System.Windows.Input;

namespace BusinessManager.App.Services;

public partial class InputDialog : Window, IParameterReceiver
{
    public string InputText { get; private set; } = string.Empty;

    public InputDialog(string message, string title, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        InputTextBox.Text = defaultValue;
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        InputText = InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OkButton_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            CancelButton_Click(sender, e);
        }
    }

    public void ReceiveParameter(object parameter)
    {
        // Handle any parameters if needed
    }
}
