using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Domain.Enums;
using BusinessManager.Infrastructure.Data;

namespace BusinessManager.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.FindAsync(id) != null;
    }
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLower();
        return await _dbSet.FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet.Where(u => u.IsActive).ToListAsync();
    }
}

public class ServiceItemRepository : Repository<ServiceItem>, IServiceItemRepository
{
    public ServiceItemRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<ServiceItem>> GetActiveServicesAsync()
    {
        return await _dbSet
            .Where(si => si.IsActive)
            .OrderBy(si => si.Category)
            .ThenBy(si => si.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceItem>> GetByCategoryAsync(ServiceCategory category)
    {
        return await _dbSet
            .Where(si => si.Category == category && si.IsActive)
            .OrderBy(si => si.Name)
            .ToListAsync();
    }
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        return await _dbSet
            .Where(p => p.IsActive && p.CurrentStock <= p.ReorderLevel)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}

public class SaleRepository : Repository<Sale>, ISaleRepository
{
    public SaleRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Sale>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(s => s.User)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.ServiceItem)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
            .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Sale>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(s => s.User)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.ServiceItem)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<Sale?> GetWithItemsAsync(int id)
    {
        return await _dbSet
            .Include(s => s.User)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.ServiceItem)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}

public class SaleItemRepository : Repository<SaleItem>, ISaleItemRepository
{
    public SaleItemRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<SaleItem>> GetBySaleIdAsync(int saleId)
    {
        return await _dbSet
            .Include(si => si.ServiceItem)
            .Include(si => si.Product)
            .Where(si => si.SaleId == saleId)
            .ToListAsync();
    }
}

public class ExpenseRepository : Repository<Expense>, IExpenseRepository
{
    public ExpenseRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Expense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(e => e.User)
            .Include(e => e.ExpenseCategory)
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Expense>> GetByCategoryIdAsync(int categoryId)
    {
        return await _dbSet
            .Include(e => e.User)
            .Include(e => e.ExpenseCategory)
            .Where(e => e.ExpenseCategoryId == categoryId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Expense>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(e => e.User)
            .Include(e => e.ExpenseCategory)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
}

public class ExpenseCategoryRepository : Repository<ExpenseCategory>, IExpenseCategoryRepository
{
    public ExpenseCategoryRepository(AppDbContext context) : base(context) { }

    public async Task<ExpenseCategory?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ec => ec.Name == name);
    }

    public async Task<IEnumerable<ExpenseCategory>> GetActiveCategoriesAsync()
    {
        return await _dbSet
            .Where(ec => ec.IsActive)
            .OrderBy(ec => ec.Name)
            .ToListAsync();
    }
}

public class InventoryMovementRepository : Repository<InventoryMovement>, IInventoryMovementRepository
{
    public InventoryMovementRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<InventoryMovement>> GetByProductAsync(int productId)
    {
        return await _dbSet
            .Include(im => im.Product)
            .Include(im => im.User)
            .Where(im => im.ProductId == productId)
            .OrderByDescending(im => im.MovementDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(im => im.Product)
            .Include(im => im.User)
            .Where(im => im.MovementDate >= startDate && im.MovementDate <= endDate)
            .OrderByDescending(im => im.MovementDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetByMovementTypeAsync(InventoryMovementType type)
    {
        return await _dbSet
            .Include(im => im.Product)
            .Include(im => im.User)
            .Where(im => im.MovementType == type)
            .OrderByDescending(im => im.MovementDate)
            .ToListAsync();
    }
}

public class SettingRepository : Repository<Setting>, ISettingRepository
{
    public SettingRepository(AppDbContext context) : base(context) { }

    public async Task<Setting?> GetByKeyAsync(string key)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await GetByKeyAsync(key);
        return setting?.Value;
    }

    public async Task<IEnumerable<Setting>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Where(s => s.Category == category)
            .OrderBy(s => s.Key)
            .ToListAsync();
    }
}

public class PriceHistoryRepository : Repository<PriceHistory>, IPriceHistoryRepository
{
    public PriceHistoryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<PriceHistory>> GetByServiceItemIdAsync(int serviceItemId)
    {
        return await _dbSet
            .Include(ph => ph.ServiceItem)
            .Include(ph => ph.User)
            .Where(ph => ph.ItemId == serviceItemId && ph.PriceType == "Service")
            .OrderByDescending(ph => ph.ChangedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PriceHistory>> GetByProductIdAsync(int productId)
    {
        return await _dbSet
            .Include(ph => ph.Product)
            .Include(ph => ph.User)
            .Where(ph => ph.ItemId == productId && ph.PriceType == "Product")
            .OrderByDescending(ph => ph.ChangedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PriceHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(ph => ph.ServiceItem)
            .Include(ph => ph.Product)
            .Include(ph => ph.User)
            .Where(ph => ph.ChangedAt >= startDate && ph.ChangedAt <= endDate)
            .OrderByDescending(ph => ph.ChangedAt)
            .ToListAsync();
    }
}

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(al => al.User)
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action)
    {
        return await _dbSet
            .Include(al => al.User)
            .Where(al => al.Action == action)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(al => al.User)
            .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync();
    }
}
