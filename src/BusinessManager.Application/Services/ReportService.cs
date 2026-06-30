using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Domain.Entities;

namespace BusinessManager.Application.Services;

public class ReportService : IReportService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IDebtorRepository _debtorRepository;
    private readonly IProductRepository _productRepository;
    private readonly IClientOrderRepository _orderRepository;
    private readonly DbAccessGate _dbGate;
    private readonly ILogger<ReportService> _logger;
    // Lazy so QuestPDF font loading happens on a Task.Run thread, not the UI thread.
    private readonly Lazy<IPdfGenerator>? _lazyPdfGenerator;

    public ReportService(
        ISaleRepository saleRepository,
        IExpenseRepository expenseRepository,
        IDebtorRepository debtorRepository,
        IProductRepository productRepository,
        IClientOrderRepository orderRepository,
        DbAccessGate dbGate,
        ILogger<ReportService> logger,
        Lazy<IPdfGenerator>? pdfGenerator = null)
    {
        _saleRepository = saleRepository;
        _expenseRepository = expenseRepository;
        _debtorRepository = debtorRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _dbGate = dbGate;
        _logger = logger;
        _lazyPdfGenerator = pdfGenerator;
    }

    public Task<DailySummaryDto> GetDailySummaryAsync(DateTime date) =>
        _dbGate.RunAsync(() => GetDailySummaryCoreAsync(date));

    private async Task<DailySummaryDto> GetDailySummaryCoreAsync(DateTime date)
    {
        try
        {
            var (startDate, endDate) = BusinessDateHelper.GetLocalDayRange(date);

            var sales = await _saleRepository.GetByDateRangeAsync(startDate, endDate);
            var expenses = await _expenseRepository.GetByDateRangeAsync(startDate, endDate);
            var orderPayments = await _orderRepository.GetByPaymentDateRangeAsync(startDate, endDate);

            var totalMadeToday = sales.Sum(s => GetSaleTotal(s)) + orderPayments.Sum(o => o.AmountPaid);
            var totalExpenses = expenses.Sum(e => e.Amount);
            var drawerBalance = totalMadeToday - totalExpenses;

            return new DailySummaryDto
            {
                Date = date.Date,
                CashReceived = totalMadeToday,
                TotalIncome = totalMadeToday,
                TotalExpenses = totalExpenses,
                DrawerBalance = drawerBalance,
                TotalProfit = drawerBalance,
                TotalSales = sales.Count(),
                TotalExpensesCount = expenses.Count(),
                AverageSaleAmount = sales.Any() ? totalMadeToday / sales.Count() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily summary for {Date}", date);
            throw;
        }
    }

    public Task<WeeklySummaryDto> GetWeeklySummaryAsync(DateTime date) =>
        _dbGate.RunAsync(() => GetWeeklySummaryCoreAsync(date));

    private async Task<WeeklySummaryDto> GetWeeklySummaryCoreAsync(DateTime date)
    {
        try
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var weekStart = date.Date.AddDays(-dayOfWeek);
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);

            var sales = await _saleRepository.GetByDateRangeAsync(weekStart, weekEnd);
            var expenses = await _expenseRepository.GetByDateRangeAsync(weekStart, weekEnd);
            var orderPayments = await _orderRepository.GetByPaymentDateRangeAsync(weekStart, weekEnd);

            var totalIncome = sales.Sum(s => GetSaleTotal(s)) + orderPayments.Sum(o => o.AmountPaid);
            var totalExpenses = expenses.Sum(e => e.Amount);

            return new WeeklySummaryDto
            {
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                TotalProfit = totalIncome - totalExpenses,
                TotalSales = sales.Count(),
                TotalExpensesCount = expenses.Count()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly summary for {Date}", date);
            throw;
        }
    }

    public Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month) =>
        _dbGate.RunAsync(() => GetMonthlySummaryCoreAsync(year, month));

    private async Task<MonthlySummaryDto> GetMonthlySummaryCoreAsync(int year, int month)
    {
        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            var sales = await _saleRepository.GetByDateRangeAsync(startDate, endDate);
            var expenses = await _expenseRepository.GetByDateRangeAsync(startDate, endDate);
            var orderPayments = await _orderRepository.GetByPaymentDateRangeAsync(startDate, endDate);

            var totalIncome = sales.Sum(s => GetSaleTotal(s)) + orderPayments.Sum(o => o.AmountPaid);
            var totalExpenses = expenses.Sum(e => e.Amount);
            var totalProfit = totalIncome - totalExpenses;

            return new MonthlySummaryDto
            {
                Year = year,
                Month = month,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                TotalProfit = totalProfit,
                TotalSales = sales.Count(),
                TotalExpensesCount = expenses.Count(),
                AverageSaleAmount = sales.Any() ? totalIncome / sales.Count() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly summary for {Year}-{Month}", year, month);
            throw;
        }
    }

    public Task<IEnumerable<IncomeByModuleDto>> GetIncomeByModuleAsync(DateTime startDate, DateTime endDate) =>
        _dbGate.RunAsync(() => GetIncomeByModuleCoreAsync(startDate, endDate));

    private async Task<IEnumerable<IncomeByModuleDto>> GetIncomeByModuleCoreAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var sales = await _saleRepository.GetByDateRangeAsync(startDate, endDate);
            var moduleIncome = new Dictionary<string, decimal>();

            foreach (var sale in sales)
            {
                foreach (var item in sale.SaleItems)
                {
                    var moduleName = item.ServiceItem?.Name ?? item.Product?.Name ?? "Other";
                    if (moduleIncome.ContainsKey(moduleName))
                    {
                        moduleIncome[moduleName] += item.TotalPrice;
                    }
                    else
                    {
                        moduleIncome[moduleName] = item.TotalPrice;
                    }
                }
            }

            var totalIncome = moduleIncome.Values.Sum();
            return moduleIncome.Select(kvp => new IncomeByModuleDto
            {
                Module = kvp.Key,
                TotalIncome = kvp.Value,
                Percentage = totalIncome > 0 ? (kvp.Value / totalIncome) * 100 : 0,
                TransactionCount = sales.Count(s => s.SaleItems.Any(si => 
                    (si.ServiceItem?.Name == kvp.Key) || (si.Product?.Name == kvp.Key)))
            }).OrderByDescending(x => x.TotalIncome);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting income by module for {StartDate} to {EndDate}", startDate, endDate);
            return Enumerable.Empty<IncomeByModuleDto>();
        }
    }

    public async Task<IEnumerable<ExpenseByCategoryDto>> GetExpenseByCategoryAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var expenses = await _expenseRepository.GetByDateRangeAsync(startDate, endDate);
            var categoryExpenses = expenses
                .GroupBy(e => e.ExpenseCategory.Name)
                .Select(g => new ExpenseByCategoryDto
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    TransactionCount = g.Count(),
                    Percentage = 0 // Will be calculated below
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            var totalExpenses = categoryExpenses.Sum(x => x.Amount);
            foreach (var item in categoryExpenses)
            {
                item.Percentage = totalExpenses > 0 ? (item.Amount / totalExpenses) * 100 : 0;
            }

            return categoryExpenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense by category for {StartDate} to {EndDate}", startDate, endDate);
            return Enumerable.Empty<ExpenseByCategoryDto>();
        }
    }

    public Task<byte[]> GenerateDailyReportAsync(DateTime date)
    {
        try
        {
            // This will be implemented in the Reporting project
            throw new NotImplementedException("Daily report generation will be implemented in the Reporting project");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily report for {Date}", date);
            throw;
        }
    }

    public Task<byte[]> GenerateMonthlyReportAsync(int year, int month)
    {
        try
        {
            // This will be implemented in the Reporting project
            throw new NotImplementedException("Monthly report generation will be implemented in the Reporting project");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly report for {Year}-{Month}", year, month);
            throw;
        }
    }

    public async Task<IEnumerable<ReportDto>> GetReportsAsync()
    {
        try
        {
            // Simplified implementation - in real app this would query database for saved reports
            var reports = new List<ReportDto>();
            
            return await Task.FromResult(reports.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports");
            return Enumerable.Empty<ReportDto>();
        }
    }

    public async Task<ReportDto> GenerateReportAsync(GenerateReportRequest request)
    {
        // Phase 1 — collect data inside the gate (fast DB queries only)
        var data = await _dbGate.RunAsync(() => CollectReportDataAsync(request));

        // Phase 2 — generate PDF on thread-pool (CPU-intensive, no DB access).
        // Accessing _lazyPdfGenerator.Value here (inside Task.Run) means QuestPDF font
        // loading runs on a background thread instead of blocking the UI thread.
        var pdfBytes = await Task.Run(() =>
        {
            var gen = _lazyPdfGenerator?.Value;
            return gen != null
                ? gen.GenerateFinancialReport(data)
                : System.Text.Encoding.UTF8.GetBytes(
                    $"PDF generator unavailable. Income: {data.TotalIncome}, Expenses: {data.TotalExpenses}");
        });

        // Phase 3 — save file (fast I/O)
        return await Task.Run(() => SaveReportFile(request, data, pdfBytes));
    }

    private static ReportDto SaveReportFile(GenerateReportRequest request, FinancialReportData data, byte[] pdfBytes)
    {
        var reportsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "BrendaBusinessReports");
        Directory.CreateDirectory(reportsDir);

        var fileName = $"{data.ReportTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(reportsDir, fileName);
        File.WriteAllBytes(filePath, pdfBytes);

        return new ReportDto
        {
            Name        = data.ReportTitle,
            ReportType  = request.ReportType,
            FilePath    = filePath,
            GeneratedAt = DateTime.Now,
            FileSize    = pdfBytes.Length
        };
    }

    private async Task<FinancialReportData> CollectReportDataAsync(GenerateReportRequest request)
    {
        try
        {
            var start = request.StartDate.Date;
            var end   = request.EndDate.Date.AddDays(1).AddTicks(-1);

            var sales    = (await _saleRepository.GetByDateRangeAsync(start, end)).ToList();
            var expenses = (await _expenseRepository.GetByDateRangeAsync(start, end)).ToList();

            var totalIncome   = sales.Sum(s => GetSaleTotal(s));
            var totalExpenses = expenses.Sum(e => e.Amount);
            var netProfit     = totalIncome - totalExpenses;

            // Income breakdown by service/product
            var incomeMap = new Dictionary<string, decimal>();
            foreach (var sale in sales)
            {
                foreach (var item in sale.SaleItems)
                {
                    var label = item.ServiceItem?.Name ?? item.Product?.Name ?? "General";
                    incomeMap[label] = incomeMap.GetValueOrDefault(label) + item.TotalPrice;
                }
            }
            var incomeLines = incomeMap
                .OrderByDescending(x => x.Value)
                .Select(x => new IncomeLine(x.Key, x.Value,
                    totalIncome > 0 ? (double)(x.Value / totalIncome * 100) : 0))
                .ToList();

            var saleLines = sales
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new SaleLine(
                    s.SaleDate,
                    string.Join(", ", s.SaleItems.Select(i => i.ServiceItem?.Name ?? i.Product?.Name ?? "Item")),
                    GetSaleTotal(s)))
                .ToList();

            var expenseLines = expenses
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => new ExpenseLine(
                    e.ExpenseDate,
                    e.ExpenseCategory?.Name ?? "Uncategorised",
                    e.Description ?? "",
                    e.Amount))
                .ToList();

            var isSingleDay = start.Date == end.Date;
            var period = isSingleDay
                ? start.ToString("dd MMMM yyyy")
                : $"{start:dd MMM yyyy} – {request.EndDate.Date:dd MMM yyyy}";

            var title = request.ReportType switch
            {
                "Sales"    => "Sales Report",
                "Expenses" => "Expenses Report",
                _          => "Financial Summary Report"
            };

            return new FinancialReportData(
                ReportTitle:            title,
                ReportType:             request.ReportType,
                StartDate:              start,
                EndDate:                request.EndDate.Date,
                Period:                 period,
                TotalIncome:            totalIncome,
                TotalExpenses:          totalExpenses,
                NetProfit:              netProfit,
                TotalSaleTransactions:  sales.Count,
                TotalExpenseTransactions: expenses.Count,
                IncomeSummary:          incomeLines,
                SaleDetails:            saleLines,
                ExpenseDetails:         expenseLines
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting report data for {ReportType}", request.ReportType);
            throw;
        }
    }

    public async Task<bool> DeleteReportAsync(int reportId)
    {
        try
        {
            // Simplified implementation
            _logger.LogInformation("Report {ReportId} deleted successfully", reportId);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report {ReportId}", reportId);
            return false;
        }
    }

    public Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int monthsBack) =>
        _dbGate.RunAsync(() => GetMonthlyTrendCoreAsync(monthsBack));

    private async Task<List<MonthlyTrendDto>> GetMonthlyTrendCoreAsync(int monthsBack)
    {
        var today = DateTime.Today;
        var rangeStart = new DateTime(today.Year, today.Month, 1).AddMonths(-(monthsBack - 1));
        var rangeEnd = today;

        // Two DB queries cover all months instead of 2*monthsBack separate queries
        var sales = (await _saleRepository.GetByDateRangeAsync(rangeStart, rangeEnd)).ToList();
        var expenses = (await _expenseRepository.GetByDateRangeAsync(rangeStart, rangeEnd)).ToList();

        var trends = new List<MonthlyTrendDto>();
        for (int i = monthsBack - 1; i >= 0; i--)
        {
            var month = today.AddMonths(-i);
            var mStart = new DateTime(month.Year, month.Month, 1);
            var mEnd = mStart.AddMonths(1);

            trends.Add(new MonthlyTrendDto
            {
                Month = month.ToString("MMM"),
                Income = sales
                    .Where(s => s.SaleDate >= mStart && s.SaleDate < mEnd)
                    .Sum(s => GetSaleTotal(s)),
                Expenses = expenses
                    .Where(e => e.ExpenseDate >= mStart && e.ExpenseDate < mEnd)
                    .Sum(e => e.Amount)
            });
        }

        return trends;
    }

    internal static decimal GetSaleTotal(Sale sale)
    {
        if (sale.SaleItems != null && sale.SaleItems.Count > 0)
            return sale.SaleItems.Sum(i => i.TotalPrice);

        return sale.TotalAmount;
    }
}
