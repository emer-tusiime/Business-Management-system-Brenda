using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using BusinessManager.App.Services;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Entities;

namespace BusinessManager.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly IInventoryService _inventoryService;
    private readonly IDebtorService _debtorService;
    private readonly ILogger<DashboardViewModel> _logger;

    private DateTime? _lastLoadedDate;

    [ObservableProperty]
    private DashboardSummaryDto _summary = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _lastUpdated = string.Empty;

    [ObservableProperty]
    private ISeries[] _incomeByModuleSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _monthlyTrendSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _monthlyTrendXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _monthlyTrendYAxes = Array.Empty<Axis>();

    public DashboardViewModel(
        IReportService reportService,
        IInventoryService inventoryService,
        IDebtorService debtorService,
        ILogger<DashboardViewModel> logger)
    {
        _reportService = reportService;
        _inventoryService = inventoryService;
        _debtorService = debtorService;
        _logger = logger;

        LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(forceReload: true));
    }

    public IRelayCommand LoadDataCommand { get; }

    public async Task LoadDataAsync(bool forceReload = false)
    {
        // Skip reload if data is already fresh for today (auto-load on navigation)
        if (!forceReload && _lastLoadedDate == DateTime.Today && Summary.TodayIncome > 0)
            return;

        try
        {
            IsLoading = true;

            var today = DateTime.Today;
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var thisMonthEnd = today;

            var summary = new DashboardSummaryDto();

            try
            {
                var dailySummary = await _reportService.GetDailySummaryAsync(today);
                summary.TodayIncome = dailySummary.TotalIncome;
                summary.TodayExpenses = dailySummary.TotalExpenses;
                summary.DrawerBalance = dailySummary.DrawerBalance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading daily summary");
            }

            try
            {
                var weeklySummary = await _reportService.GetWeeklySummaryAsync(today);
                summary.ThisWeekIncome = weeklySummary.TotalIncome;
                summary.ThisWeekExpenses = weeklySummary.TotalExpenses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading weekly summary");
            }

            try
            {
                var monthlySummary = await _reportService.GetMonthlySummaryAsync(today.Year, today.Month);
                summary.ThisMonthIncome = monthlySummary.TotalIncome;
                summary.ThisMonthExpenses = monthlySummary.TotalExpenses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading monthly summary");
            }

            try
            {
                var incomeByModule = await _reportService.GetIncomeByModuleAsync(thisMonthStart, thisMonthEnd);
                summary.IncomeByModule = incomeByModule.ToList();
                summary.TopSellingItem = summary.IncomeByModule.FirstOrDefault()?.Module;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading income by module");
            }

            try
            {
                var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();
                summary.LowStockCount = lowStockProducts.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading low stock products");
            }

            try
            {
                summary.TotalOutstandingDebt = await _debtorService.GetTotalOutstandingAsync();
                summary.CustomerDebts = (await _debtorService.GetCustomerDebtSummariesAsync()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading debtor summaries");
            }

            try
            {
                summary.MonthlyTrend = await _reportService.GetMonthlyTrendAsync(6);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading monthly trend");
            }

            Summary = summary;
            UpdateIncomeByModuleChart(Summary.IncomeByModule);
            UpdateMonthlyTrendChart(Summary.MonthlyTrend);
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            _lastLoadedDate = DateTime.Today;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateIncomeByModuleChart(List<IncomeByModuleDto> incomeByModule)
    {
        if (!incomeByModule.Any())
        {
            IncomeByModuleSeries = new ISeries[]
            {
                new PieSeries<double>
                {
                    Values = new double[] { 1 },
                    Name = "No Data",
                    Fill = new SolidColorPaint(SKColors.Gray)
                }
            };
            return;
        }

        var colors = new[]
        {
            SKColors.Blue, SKColors.Green, SKColors.Orange, SKColors.Purple,
            SKColors.Red, SKColors.Cyan, SKColors.Pink, SKColors.Yellow
        };

        IncomeByModuleSeries = incomeByModule.Select((item, index) =>
        {
            var color = colors[index % colors.Length];
            return new PieSeries<double>
            {
                Values = new double[] { (double)item.TotalIncome },
                Name = item.Module,
                Fill = new SolidColorPaint(color),
                Stroke = new SolidColorPaint(SKColors.White, 2),
                InnerRadius = 65,
                MaxOuterRadius = 120
            };
        }).ToArray<ISeries>();
    }

    private void UpdateMonthlyTrendChart(List<MonthlyTrendDto> monthlyTrend)
    {
        if (!monthlyTrend.Any())
        {
            MonthlyTrendSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new double[] { 0 },
                    Name = "No Data",
                    GeometrySize = 10,
                    Stroke = new SolidColorPaint(SKColors.Gray, 2),
                    Fill = null
                }
            };
            return;
        }

        var incomeSeries = new LineSeries<double>
        {
            Values = monthlyTrend.Select(t => (double)t.Income).ToArray(),
            Name = "Income",
            GeometrySize = 10,
            Stroke = new SolidColorPaint(SKColors.Green, 3),
            Fill = null
        };

        var expenseSeries = new LineSeries<double>
        {
            Values = monthlyTrend.Select(t => (double)t.Expenses).ToArray(),
            Name = "Expenses",
            GeometrySize = 10,
            Stroke = new SolidColorPaint(SKColors.Red, 3),
            Fill = null
        };

        MonthlyTrendSeries = new ISeries[] { incomeSeries, expenseSeries };

        MonthlyTrendXAxes = new Axis[]
        {
            new Axis
            {
                Labels = monthlyTrend.Select(t => t.Month).ToArray(),
                LabelsRotation = 0,
                LabelsPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TicksPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TicksAtCenter = true
            }
        };

        MonthlyTrendYAxes = new Axis[]
        {
            new Axis
            {
                LabelsRotation = 0,
                LabelsPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TicksPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                Labeler = value => ((int)value).ToString("N0")
            }
        };
    }


}
