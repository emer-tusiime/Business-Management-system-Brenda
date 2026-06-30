using System;
using System.Collections.Generic;

namespace BusinessManager.Application.Services;

public interface IPdfGenerator
{
    byte[] GenerateFinancialReport(FinancialReportData data);
}

public record FinancialReportData(
    string ReportTitle,
    string ReportType,
    DateTime StartDate,
    DateTime EndDate,
    string Period,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetProfit,
    int TotalSaleTransactions,
    int TotalExpenseTransactions,
    List<IncomeLine> IncomeSummary,
    List<SaleLine> SaleDetails,
    List<ExpenseLine> ExpenseDetails
);

public record IncomeLine(string Label, decimal Amount, double Percentage);
public record SaleLine(DateTime Date, string Description, decimal Amount);
public record ExpenseLine(DateTime Date, string Category, string Description, decimal Amount);
