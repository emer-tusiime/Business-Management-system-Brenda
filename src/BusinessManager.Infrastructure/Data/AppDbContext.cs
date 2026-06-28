using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessManager.Domain.Entities;
using BusinessManager.Infrastructure.Data.Configurations;

namespace BusinessManager.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<ServiceItem> ServiceItems { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Debtor> Debtors { get; set; }
    public DbSet<DebtPayment> DebtPayments { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<PriceHistory> PriceHistory { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceItemConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new SaleConfiguration());
        modelBuilder.ApplyConfiguration(new SaleItemConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
        modelBuilder.ApplyConfiguration(new DebtorConfiguration());
        modelBuilder.ApplyConfiguration(new DebtPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryMovementConfiguration());
        modelBuilder.ApplyConfiguration(new SettingConfiguration());
        modelBuilder.ApplyConfiguration(new PriceHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());

        // Seed data
        var seedData = new SeedData();
        modelBuilder.ApplyConfiguration<User>((IEntityTypeConfiguration<User>)seedData);
        modelBuilder.ApplyConfiguration<ServiceItem>((IEntityTypeConfiguration<ServiceItem>)seedData);
        modelBuilder.ApplyConfiguration<Product>((IEntityTypeConfiguration<Product>)seedData);
        modelBuilder.ApplyConfiguration<ExpenseCategory>((IEntityTypeConfiguration<ExpenseCategory>)seedData);
        modelBuilder.ApplyConfiguration<Setting>((IEntityTypeConfiguration<Setting>)seedData);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                ((BaseEntity)entry.Entity).CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                ((BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
