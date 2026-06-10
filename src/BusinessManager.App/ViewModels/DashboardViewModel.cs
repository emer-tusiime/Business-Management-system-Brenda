using System;
using System.Collections.Generic;
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

namespace BusinessManager.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly IInventoryService _inventoryService;
    private readonly ISettingService _settingService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<DashboardViewModel> _logger;

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
        ISettingService settingService,
        IDialogService dialogService,
        ILogger<DashboardViewModel> logger)
    {
        _reportService = reportService;
        _inventoryService = inventoryService;
        _settingService = settingService;
        _dialogService = dialogService;
        _logger = logger;

        LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
        SetDrawerOpeningBalanceCommand = new RelayCommand(async () => await SetDrawerOpeningBalanceAsync());
    }

    public IRelayCommand LoadDataCommand { get; }
    public IRelayCommand SetDrawerOpeningBalanceCommand { get; }

    public async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            var today = DateTime.Today;
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1).AddTicks(-1);

            var dailySummary = await _reportService.GetDailySummaryAsync(today);
            var weeklySummary = await _reportService.GetWeeklySummaryAsync(today);
            var monthlySummary = await _reportService.GetMonthlySummaryAsync(today.Year, today.Month);
            var incomeByModule = await _reportService.GetIncomeByModuleAsync(thisMonthStart, thisMonthEnd);
            var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();

            var openingBalanceText = await _settingService.GetSettingAsync("DrawerOpeningBalance");
            var openingBalance = decimal.TryParse(openingBalanceText, out var parsedBalance) ? parsedBalance : 0m;
            var drawerBalance = openingBalance + dailySummary.TotalIncome - dailySummary.TotalExpenses;

            Summary = new DashboardSummaryDto
            {
                TodayIncome = dailySummary.TotalIncome,
                TodayExpenses = dailySummary.TotalExpenses,
                TodayProfit = dailySummary.TotalProfit,
                DrawerOpeningBalance = openingBalance,
                DrawerBalance = drawerBalance,
                ThisWeekIncome = weeklySummary.TotalIncome,
                ThisWeekExpenses = weeklySummary.TotalExpenses,
                ThisWeekProfit = weeklySummary.TotalProfit,
                ThisMonthIncome = monthlySummary.TotalIncome,
                ThisMonthExpenses = monthlySummary.TotalExpenses,
                ThisMonthProfit = monthlySummary.TotalProfit,
                LowStockCount = lowStockProducts.Count(),
                TopSellingItem = incomeByModule.FirstOrDefault()?.Module,
                IncomeByModule = incomeByModule.ToList(),
                MonthlyTrend = await GetMonthlyTrendAsync()
            };

            UpdateIncomeByModuleChart(Summary.IncomeByModule);
            UpdateMonthlyTrendChart(Summary.MonthlyTrend);

            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
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

    private async Task SetDrawerOpeningBalanceAsync()
    {
        var input = _dialogService.ShowInputDialog(
            "Enter the cash amount currently in the drawer at the start of today:",
            "Set Drawer Opening Balance",
            Summary.DrawerOpeningBalance.ToString("0"));

        if (string.IsNullOrWhiteSpace(input) || !decimal.TryParse(input, out var amount) || amount < 0)
        {
            return;
        }

        await _settingService.SetSettingAsync("DrawerOpeningBalance", amount);
        await LoadDataAsync();
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

        var profitSeries = new LineSeries<double>
        {
            Values = monthlyTrend.Select(t => (double)t.Profit).ToArray(),
            Name = "Profit",
            GeometrySize = 10,
            Stroke = new SolidColorPaint(SKColors.Blue, 3),
            Fill = null
        };

        MonthlyTrendSeries = new ISeries[] { incomeSeries, expenseSeries, profitSeries };

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

    private async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync()
    {
        var trends = new List<MonthlyTrendDto>();
        var today = DateTime.Today;

        for (int i = 5; i >= 0; i--)
        {
            var month = today.AddMonths(-i);
            var summary = await _reportService.GetMonthlySummaryAsync(month.Year, month.Month);

            trends.Add(new MonthlyTrendDto
            {
                Month = month.ToString("MMM"),
                Income = summary.TotalIncome,
                Expenses = summary.TotalExpenses,
                Profit = summary.TotalProfit
            });
        }

        return trends;
    }
}
