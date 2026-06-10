using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;

namespace BusinessManager.App.Views.Welcome;

public partial class WelcomeWindow : Window
{
    private readonly DispatcherTimer _timer;

    public WelcomeWindow()
    {
        InitializeComponent();
        
        // Setup timer to update date/time
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        
        UpdateDateTime();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateDateTime();
    }

    private void UpdateDateTime()
    {
        DateTimeText.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy HH:mm:ss");
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _timer?.Stop();
        base.OnClosing(e);
    }
}
