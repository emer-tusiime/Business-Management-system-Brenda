using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System; using System.Collections.Generic; using System.Threading.Tasks; using CommunityToolkit.Mvvm.ComponentModel; using CommunityToolkit.Mvvm.Input; using Microsoft.Extensions.Logging; using BusinessManager.Domain.Interfaces; using BusinessManager.Domain.Entities; using BusinessManager.Domain.DTOs; using BusinessManager.Domain.Enums; using BusinessManager.Application.Services;
using BusinessManager.Domain.DTOs;

namespace BusinessManager.App.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReportsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ReportDto> _reports = new();

    [ObservableProperty]
    private ReportDto? _selectedReport;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _reportType = "Sales";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isGenerating;

    public ReportsViewModel(
        IReportService reportService,
        INotificationService notificationService,
        ILogger<ReportsViewModel> logger)
    {
        _reportService = reportService;
        _notificationService = notificationService;
        _logger = logger;

        LoadReportsCommand = new RelayCommand(async () => await LoadReportsAsync());
        GenerateReportCommand = new RelayCommand(async () => await GenerateReportAsync(), CanGenerateReport);
        ViewReportCommand = new RelayCommand<ReportDto>(async (report) => await ViewReportAsync(report));
        DeleteReportCommand = new RelayCommand<ReportDto>(async (report) => await DeleteReportAsync(report));
        RefreshCommand = new RelayCommand(async () => await LoadReportsAsync());
    }

    public IRelayCommand LoadReportsCommand { get; }
    public IRelayCommand GenerateReportCommand { get; }
    public IRelayCommand<ReportDto> ViewReportCommand { get; }
    public IRelayCommand<ReportDto> DeleteReportCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadReportsAsync();
    }

    private async Task LoadReportsAsync()
    {
        try
        {
            IsLoading = true;
            
            var reports = await _reportService.GetReportsAsync();
            Reports = new ObservableCollection<ReportDto>(reports.OrderByDescending(r => r.GeneratedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reports");
            _notificationService.ShowError("Error loading reports");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanGenerateReport()
    {
        return !string.IsNullOrWhiteSpace(ReportType) && StartDate <= EndDate;
    }

    private async Task GenerateReportAsync()
    {
        try
        {
            if (!CanGenerateReport()) return;

            IsGenerating = true;

            var reportRequest = new GenerateReportRequest
            {
                ReportType = ReportType,
                StartDate = StartDate,
                EndDate = EndDate.AddDays(1).AddTicks(-1) // Include end date
            };

            var report = await _reportService.GenerateReportAsync(reportRequest);
            _notificationService.ShowSuccess("Report generated successfully");
            
            await LoadReportsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            _notificationService.ShowError("Error generating report");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task ViewReportAsync(ReportDto? report)
    {
        try
        {
            if (report == null) return;

            // Open the PDF file
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = report.FilePath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing report");
            _notificationService.ShowError("Error viewing report");
        }
    }

    private async Task DeleteReportAsync(ReportDto? report)
    {
        try
        {
            if (report == null) return;

            if (!_notificationService.ShowConfirmation($"Are you sure you want to delete this report: {report.Name}?"))
                return;

            await _reportService.DeleteReportAsync(report.Id);
            _notificationService.ShowSuccess("Report deleted successfully");
            await LoadReportsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report");
            _notificationService.ShowError("Error deleting report");
        }
    }

    partial void OnStartDateChanged(DateTime value)
    {
        if (StartDate > EndDate)
        {
            EndDate = StartDate;
        }
    }

    partial void OnEndDateChanged(DateTime value)
    {
        if (EndDate < StartDate)
        {
            StartDate = EndDate;
        }
    }
}
