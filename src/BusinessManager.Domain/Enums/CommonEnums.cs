namespace BusinessManager.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Attendant = 2
}

public enum ServiceCategory
{
    Typing = 1,
    Printing = 2,
    Photocopy = 3,
    Binding = 4,
    Sealing = 5,
    Labelling = 6,
    EmailCreation = 7,
    PassportApplication = 8,
    Branding = 9,
    Fridge = 10,
    Scanning = 11,
    Laminating = 12,
    Other = 99
}

public enum OrderStatus
{
    Pending = 1,
    Ready = 2,
    Delivered = 3
}

public enum TransactionType
{
    Sale = 1,
    Expense = 2
}

public enum InventoryMovementType
{
    StockIn = 1,
    StockOut = 2,
    Adjustment = 3
}

public enum ReportType
{
    DailyIncome = 1,
    DailyExpense = 2,
    DailyProfit = 3,
    MonthlyIncome = 4,
    MonthlyExpense = 5,
    MonthlyProfit = 6,
    IncomeByModule = 7,
    ExpenseByCategory = 8,
    StockMovement = 9,
    BestSellingItems = 10
}
