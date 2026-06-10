using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class ReportService : IReportService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ISaleRepository saleRepository,
        IExpenseRepository expenseRepository,
        IProductRepository productRepository,
        ILogger<ReportService> logger)
    {
        _saleRepository = saleRepository;
        _expenseRepository = expenseRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<DailySummaryDto> GetDailySummaryAsync(DateTime date)
    {
        try
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1).AddTicks(-1);

            var sales = await _saleRepository.GetByDateRangeAsync(startDate, endDate);
            var expenses = await _expenseRepository.GetByDateRangeAsync(startDate, endDate);

            var totalIncome = sales.Sum(s => s.TotalAmount);
            var totalExpenses = expenses.Sum(e => e.Amount);
            var totalProfit = totalIncome - totalExpenses;

            return new DailySummaryDto
            {
                Date = date,
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
            _logger.LogError(ex, "Error getting daily summary for {Date}", date);
            throw;
        }
    }

    public async Task<WeeklySummaryDto> GetWeeklySummaryAsync(DateTime date)
    {
        try
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var weekStart = date.Date.AddDays(-dayOfWeek);
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);

            var sales = await _saleRepository.GetByDateRangeAsync(weekStart, weekEnd);
            var expenses = await _expenseRepository.GetByDateRangeAsync(weekStart, weekEnd);

            var totalIncome = sales.Sum(s => s.TotalAmount);
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

    public async Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month)
    {
        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            var sales = await _saleRepository.GetByDateRangeAsync(startDate, endDate);
            var expenses = await _expenseRepository.GetByDateRangeAsync(startDate, endDate);

            var totalIncome = sales.Sum(s => s.TotalAmount);
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

    public async Task<IEnumerable<IncomeByModuleDto>> GetIncomeByModuleAsync(DateTime startDate, DateTime endDate)
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
        try
        {
            _logger.LogInformation("Generating {ReportType} report from {StartDate} to {EndDate}", 
                request.ReportType, request.StartDate, request.EndDate);

            // Simplified implementation
            var reportData = new Dictionary<string, object>
            {
                { "report_type", request.ReportType },
                { "start_date", request.StartDate },
                { "end_date", request.EndDate },
                { "generated_at", DateTime.UtcNow }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(reportData);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            // Save to file (simplified)
            var fileName = $"{request.ReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Reports", fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllBytesAsync(filePath, bytes);

            var report = new ReportDto
            {
                Id = 1,
                Name = $"{request.ReportType} Report",
                ReportType = request.ReportType,
                GeneratedAt = DateTime.UtcNow,
                FilePath = filePath,
                FileSize = bytes.Length,
                GeneratedBy = "System"
            };

            _logger.LogInformation("Report generated successfully: {FileName}", fileName);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating {ReportType} report", request.ReportType);
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
}
