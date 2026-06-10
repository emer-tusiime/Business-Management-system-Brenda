using System;
using System.Collections.Generic;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Domain.DTOs;

public class DailySummaryDto
{
    public DateTime Date { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalProfit { get; set; }
    public int TotalSales { get; set; }
    public int TotalExpensesCount { get; set; }
    public decimal AverageSaleAmount { get; set; }
}

public class MonthlySummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalProfit { get; set; }
    public int TotalSales { get; set; }
    public int TotalExpensesCount { get; set; }
    public decimal AverageSaleAmount { get; set; }
}

public class WeeklySummaryDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalProfit { get; set; }
    public int TotalSales { get; set; }
    public int TotalExpensesCount { get; set; }
}

public class IncomeByModuleDto
{
    public string Module { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

public class ExpenseByCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

public class DashboardSummaryDto
{
    public decimal TodayIncome { get; set; }
    public decimal TodayExpenses { get; set; }
    public decimal TodayProfit { get; set; }
    public decimal DrawerOpeningBalance { get; set; }
    public decimal DrawerBalance { get; set; }
    public decimal ThisWeekIncome { get; set; }
    public decimal ThisWeekExpenses { get; set; }
    public decimal ThisWeekProfit { get; set; }
    public decimal ThisMonthIncome { get; set; }
    public decimal ThisMonthExpenses { get; set; }
    public decimal ThisMonthProfit { get; set; }
    public int LowStockCount { get; set; }
    public string? TopSellingItem { get; set; }
    public List<IncomeByModuleDto> IncomeByModule { get; set; } = new();
    public List<MonthlyTrendDto> MonthlyTrend { get; set; } = new();
}

public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
}

public class SaleDto
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<SaleItemDto> SaleItems { get; set; } = new();
}

public class SaleItemDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ServiceName { get; set; }
    public string? ProductName { get; set; }
}

public class ExpenseDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string PaidBy { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
    public bool IsLowStock => CurrentStock <= ReorderLevel;
    public decimal StockValue => CurrentStock * CostPrice;
}

public class ServiceItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
    public bool IsFlexiblePrice { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class ReportDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
}

public class BackupInfo
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreateBackupRequest
{
    public string Description { get; set; } = string.Empty;
    public bool IncludeUsers { get; set; }
    public bool IncludeSettings { get; set; }
    public bool IncludeData { get; set; }
}

public class GenerateReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateUserRequest
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

public class InventoryMovementDto
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }
    public InventoryMovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
}
