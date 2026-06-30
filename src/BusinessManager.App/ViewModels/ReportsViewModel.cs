using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.App.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReportsViewModel> _logger;

    [ObservableProperty] private string _selectedReportType = "Financial";
    [ObservableProperty] private string _selectedPeriod = "This Month";
    [ObservableProperty] private DateTime _customStartDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _customEndDate = DateTime.Today;
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private bool _showCustomDates;
    [ObservableProperty] private ObservableCollection<GeneratedReportItem> _recentReports = new();
    [ObservableProperty] private GeneratedReportItem? _lastReport;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public string[] ReportTypes { get; } = { "Financial", "Sales", "Expenses" };
    public string[] PeriodOptions { get; } = { "Today", "This Week", "This Month", "Custom" };

    public ReportsViewModel(
        IReportService reportService,
        INotificationService notificationService,
        ILogger<ReportsViewModel> logger)
    {
        _reportService = reportService;
        _notificationService = notificationService;
        _logger = logger;

        GenerateAndOpenCommand = new RelayCommand(async () => await GenerateAsync(openAfter: true), () => !IsGenerating);
        GenerateAndSaveCommand = new RelayCommand(async () => await GenerateAsync(openAfter: false), () => !IsGenerating);
        PrintReportCommand = new RelayCommand<GeneratedReportItem>(Print);
        OpenReportCommand  = new RelayCommand<GeneratedReportItem>(Open);
        RefreshCommand     = new RelayCommand(() => { });
    }

    public IRelayCommand GenerateAndOpenCommand { get; }
    public IRelayCommand GenerateAndSaveCommand { get; }
    public IRelayCommand<GeneratedReportItem> PrintReportCommand { get; }
    public IRelayCommand<GeneratedReportItem> OpenReportCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public Task InitializeAsync() => Task.CompletedTask;

    partial void OnSelectedPeriodChanged(string value)
    {
        ShowCustomDates = value == "Custom";
    }

    private (DateTime start, DateTime end) ResolveDates()
    {
        return SelectedPeriod switch
        {
            "Today"      => (DateTime.Today, DateTime.Today),
            "This Week"  => (DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today),
            "This Month" => (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today),
            _            => (CustomStartDate.Date, CustomEndDate.Date)
        };
    }

    private async Task GenerateAsync(bool openAfter)
    {
        try
        {
            IsGenerating = true;
            ((RelayCommand)GenerateAndOpenCommand).NotifyCanExecuteChanged();
            ((RelayCommand)GenerateAndSaveCommand).NotifyCanExecuteChanged();
            StatusMessage = "Generating report, please wait…";

            var (start, end) = ResolveDates();
            var request = new GenerateReportRequest
            {
                ReportType = SelectedReportType,
                StartDate  = start,
                EndDate    = end.AddDays(1).AddTicks(-1)
            };

            var report = await _reportService.GenerateReportAsync(request);
            var item   = new GeneratedReportItem(report.Name, report.ReportType, report.FilePath,
                report.GeneratedAt, report.FileSize);
            RecentReports.Insert(0, item);
            LastReport = item;

            StatusMessage = $"Saved: {System.IO.Path.GetFileName(report.FilePath)}";
            _notificationService.ShowSuccess("Report generated successfully");

            if (openAfter) Open(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            StatusMessage = "Failed to generate report.";
            _notificationService.ShowError("Report generation failed: " + ex.Message);
        }
        finally
        {
            IsGenerating = false;
            ((RelayCommand)GenerateAndOpenCommand).NotifyCanExecuteChanged();
            ((RelayCommand)GenerateAndSaveCommand).NotifyCanExecuteChanged();
        }
    }

    private static void Open(GeneratedReportItem? item)
    {
        if (item == null || !System.IO.File.Exists(item.FilePath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = item.FilePath, UseShellExecute = true
            });
        }
        catch { }
    }

    private static void Print(GeneratedReportItem? item)
    {
        if (item == null || !System.IO.File.Exists(item.FilePath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = item.FilePath, Verb = "print", UseShellExecute = true, CreateNoWindow = true
            });
        }
        catch { Open(item); }
    }
}

public record GeneratedReportItem(
    string Name,
    string ReportType,
    string FilePath,
    DateTime GeneratedAt,
    long FileSize)
{
    public string SizeText => FileSize < 1024 * 1024
        ? $"{FileSize / 1024.0:F1} KB"
        : $"{FileSize / (1024.0 * 1024):F2} MB";

    public string GeneratedAtText => GeneratedAt.ToString("dd/MM/yyyy HH:mm");
    public string FolderPath => System.IO.Path.GetDirectoryName(FilePath) ?? "";
}
