using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Domain.Interfaces;

public interface IAuthService
{
    Task<(User? user, string? errorMessage)> LoginAsync(string username, string password);
    Task<User> RegisterAsync(User user, string password);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> SetPasswordAsync(int userId, string newPassword);
    Task<User> UpdateUserAsync(User user);
    Task<bool> UserExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
}

public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(CreateUserRequest request);
    Task<User> UpdateUserAsync(UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int userId);
    Task<bool> ResetPasswordAsync(int userId, string newPassword);
}

public interface ISaleService
{
    Task<Sale> CreateSaleAsync(Sale sale, IEnumerable<SaleItem> saleItems);
    Task<Sale?> GetSaleByIdAsync(int id);
    Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<string> GenerateReceiptNumberAsync();
    Task<bool> DeleteSaleAsync(int saleId);
}

public interface IExpenseService
{
    Task<Expense> CreateExpenseAsync(Expense expense);
    Task<Expense?> GetExpenseByIdAsync(int id);
    Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Expense> UpdateExpenseAsync(Expense expense);
    Task<bool> DeleteExpenseAsync(int expenseId);
}

public interface IInventoryService
{
    Task<InventoryMovement> AddStockAsync(int productId, int quantity, decimal unitCost, string reason, int userId);
    Task<InventoryMovement> RemoveStockAsync(int productId, int quantity, string reason, int userId);
    Task<InventoryMovement> AdjustStockAsync(int productId, int newQuantity, string reason, int userId);
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
    Task<Product> UpdateProductAsync(Product product);
    Task<IEnumerable<InventoryMovement>> GetProductMovementsAsync(int productId);
}

public interface IReportService
{
    Task<DailySummaryDto> GetDailySummaryAsync(DateTime date);
    Task<WeeklySummaryDto> GetWeeklySummaryAsync(DateTime date);
    Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month);
    Task<IEnumerable<IncomeByModuleDto>> GetIncomeByModuleAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<ExpenseByCategoryDto>> GetExpenseByCategoryAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateDailyReportAsync(DateTime date);
    Task<byte[]> GenerateMonthlyReportAsync(int year, int month);
    Task<IEnumerable<ReportDto>> GetReportsAsync();
    Task<ReportDto> GenerateReportAsync(GenerateReportRequest request);
    Task<bool> DeleteReportAsync(int reportId);
}

public interface ISettingService
{
    Task<string?> GetSettingAsync(string key);
    Task<T?> GetSettingAsync<T>(string key);
    Task SetSettingAsync(string key, string value);
    Task SetSettingAsync<T>(string key, T value);
    Task<IEnumerable<Setting>> GetAllSettingsAsync();
    Task<bool> DeleteSettingAsync(string key);
}

public interface IBackupService
{
    Task<byte[]> CreateBackupAsync();
    Task RestoreBackupAsync(byte[] backupData);
    Task<string> SaveBackupToFileAsync(byte[] backupData, string filePath);
    Task<byte[]> LoadBackupFromFileAsync(string filePath);
    Task<IEnumerable<BackupInfo>> GetBackupsAsync();
    Task<BackupInfo> CreateBackupAsync(CreateBackupRequest request);
    Task RestoreBackupAsync(string filePath);
    Task<bool> DeleteBackupAsync(int backupId);
}

public interface INotificationService
{
    void ShowInfo(string message);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
    Task<bool> ShowConfirmationAsync(string message);
    bool ShowConfirmation(string message);
}
