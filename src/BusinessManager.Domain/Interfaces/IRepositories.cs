using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<int> CountAsync();
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
}

public interface IServiceItemRepository : IRepository<ServiceItem>
{
    Task<IEnumerable<ServiceItem>> GetActiveServicesAsync();
    Task<IEnumerable<ServiceItem>> GetByCategoryAsync(ServiceCategory category);
}

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetActiveProductsAsync();
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
}

public interface ISaleRepository : IRepository<Sale>
{
    Task<IEnumerable<Sale>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Sale>> GetByUserIdAsync(int userId);
    Task<Sale?> GetWithItemsAsync(int id);
}

public interface ISaleItemRepository : IRepository<SaleItem>
{
    Task<IEnumerable<SaleItem>> GetBySaleIdAsync(int saleId);
}

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IEnumerable<Expense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Expense>> GetByCategoryIdAsync(int categoryId);
    Task<IEnumerable<Expense>> GetByUserIdAsync(int userId);
}

public interface IExpenseCategoryRepository : IRepository<ExpenseCategory>
{
    Task<ExpenseCategory?> GetByNameAsync(string name);
    Task<IEnumerable<ExpenseCategory>> GetActiveCategoriesAsync();
}

public interface IInventoryMovementRepository : IRepository<InventoryMovement>
{
    Task<IEnumerable<InventoryMovement>> GetByProductAsync(int productId);
    Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<InventoryMovement>> GetByMovementTypeAsync(InventoryMovementType type);
}

public interface ISettingRepository : IRepository<Setting>
{
    Task<Setting?> GetByKeyAsync(string key);
    Task<string?> GetValueAsync(string key);
    Task<IEnumerable<Setting>> GetByCategoryAsync(string category);
}

public interface IPriceHistoryRepository : IRepository<PriceHistory>
{
    Task<IEnumerable<PriceHistory>> GetByServiceItemIdAsync(int serviceItemId);
    Task<IEnumerable<PriceHistory>> GetByProductIdAsync(int productId);
    Task<IEnumerable<PriceHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId);
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}

public interface IDebtorRepository : IRepository<Debtor>
{
    Task<IEnumerable<Debtor>> GetActiveAsync();
    Task<IEnumerable<Debtor>> GetByCustomerNameAsync(string customerName);
    Task<decimal> GetTotalOutstandingAsync();
    Task<decimal> GetPaymentsTotalForDateRangeAsync(DateTime startDate, DateTime endDate);
    Task AddPaymentAsync(DebtPayment payment);
}

public interface ISavingRepository : IRepository<Saving>
{
    Task<IEnumerable<Saving>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetTotalForDateAsync(DateTime date);
    Task<decimal> GetTotalForDateRangeAsync(DateTime startDate, DateTime endDate);
}

public interface IClientOrderRepository : IRepository<ClientOrder>
{
    Task<IEnumerable<ClientOrder>> GetByStatusAsync(OrderStatus status);
    Task<IEnumerable<ClientOrder>> GetDueTodayAsync();
    Task<IEnumerable<ClientOrder>> GetOverdueAsync();
    Task<IEnumerable<ClientOrder>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<ClientOrder>> GetPendingAsync();
}
