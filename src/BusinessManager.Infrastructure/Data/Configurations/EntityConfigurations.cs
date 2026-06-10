using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.Email)
            .HasMaxLength(255);
            
        builder.Property(u => u.Role)
            .HasConversion<int>();
            
        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique(true);
        
        builder.HasMany(u => u.Sales)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(u => u.Expenses)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(u => u.InventoryMovements)
            .WithOne(im => im.User)
            .HasForeignKey(im => im.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> builder)
    {
        builder.ToTable("ServiceItems");
        
        builder.HasKey(si => si.Id);
        
        builder.Property(si => si.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(si => si.Description)
            .HasMaxLength(500);
            
        builder.Property(si => si.Category)
            .HasConversion<int>();
            
        builder.Property(si => si.DefaultPrice)
            .HasPrecision(18, 2);
            
        builder.Property(si => si.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasMany(si => si.SaleItems)
            .WithOne(sli => sli.ServiceItem)
            .HasForeignKey(sli => sli.ServiceItemId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasMany(si => si.PriceHistory)
            .WithOne(ph => ph.ServiceItem)
            .HasForeignKey(ph => ph.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(p => p.Description)
            .HasMaxLength(500);
            
        builder.Property(p => p.SKU)
            .HasMaxLength(100);
            
        builder.Property(p => p.BuyingPrice)
            .HasPrecision(18, 2);
            
        builder.Property(p => p.SellingPrice)
            .HasPrecision(18, 2);
            
        builder.Property(p => p.Supplier)
            .HasMaxLength(255);
            
        builder.Property(p => p.Unit)
            .HasMaxLength(100)
            .HasDefaultValue("pcs");
            
        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasIndex(p => p.SKU).IsUnique(true);
        
        builder.HasMany(p => p.SaleItems)
            .WithOne(sli => sli.Product)
            .HasForeignKey(sli => sli.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasMany(p => p.InventoryMovements)
            .WithOne(im => im.Product)
            .HasForeignKey(im => im.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(p => p.PriceHistory)
            .WithOne(ph => ph.Product)
            .HasForeignKey(ph => ph.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(s => s.Subtotal)
            .HasPrecision(18, 2);
            
        builder.Property(s => s.TaxAmount)
            .HasPrecision(18, 2);
            
        builder.Property(s => s.DiscountAmount)
            .HasPrecision(18, 2);
            
        builder.Property(s => s.TotalAmount)
            .HasPrecision(18, 2);
            
        builder.Property(s => s.AmountPaid)
            .HasPrecision(18, 2);
            
        builder.Property(s => s.ChangeAmount)
            .HasPrecision(18, 2);
            
        builder.Property(s => s.Notes)
            .HasMaxLength(500);
            
        builder.Property(s => s.CustomerName)
            .HasMaxLength(100);
            
        builder.Property(s => s.CustomerPhone)
            .HasMaxLength(20);
            
        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasIndex(s => s.ReceiptNumber).IsUnique();
        
        builder.HasOne(s => s.User)
            .WithMany(u => u.Sales)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(s => s.SaleItems)
            .WithOne(si => si.Sale)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");
        
        builder.HasKey(si => si.Id);
        
        builder.Property(si => si.UnitPrice)
            .HasPrecision(18, 2);
            
        builder.Property(si => si.TotalPrice)
            .HasPrecision(18, 2);
            
        builder.Property(si => si.Description)
            .HasMaxLength(500);
            
        builder.Property(si => si.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasOne(si => si.Sale)
            .WithMany(s => s.SaleItems)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(si => si.ServiceItem)
            .WithMany(si => si.SaleItems)
            .HasForeignKey(si => si.ServiceItemId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(si => si.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");
        
        builder.HasKey(ec => ec.Id);
        
        builder.Property(ec => ec.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(ec => ec.Description)
            .HasMaxLength(500);
            
        builder.Property(ec => ec.Color)
            .HasMaxLength(20)
            .HasDefaultValue("#3498db");
            
        builder.Property(ec => ec.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasMany(ec => ec.Expenses)
            .WithOne(e => e.ExpenseCategory)
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);
            
        builder.Property(e => e.PaidBy)
            .HasMaxLength(100);
            
        builder.Property(e => e.Notes)
            .HasMaxLength(500);
            
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.HasOne(e => e.ExpenseCategory)
            .WithMany(ec => ec.Expenses)
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.User)
            .WithMany(u => u.Expenses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        
        builder.HasKey(im => im.Id);
        
        builder.Property(im => im.MovementType)
            .HasConversion<int>();
            
        builder.Property(im => im.Reason)
            .HasMaxLength(500);
            
        builder.Property(im => im.ReferenceNumber)
            .HasMaxLength(100);
            
        builder.Property(im => im.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasOne(im => im.Product)
            .WithMany(p => p.InventoryMovements)
            .HasForeignKey(im => im.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(im => im.User)
            .WithMany(u => u.InventoryMovements)
            .HasForeignKey(im => im.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(1000);
            
        builder.Property(s => s.Description)
            .HasMaxLength(500);
            
        builder.Property(s => s.Category)
            .HasMaxLength(50);
            
        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasIndex(s => s.Key).IsUnique();
    }
}

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistory");
        
        builder.HasKey(ph => ph.Id);
        
        builder.Property(ph => ph.PriceType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(ph => ph.OldPrice)
            .HasPrecision(18, 2);
            
        builder.Property(ph => ph.NewPrice)
            .HasPrecision(18, 2);
            
        builder.Property(ph => ph.Reason)
            .HasMaxLength(500);
            
        builder.Property(ph => ph.ChangedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasOne(ph => ph.User)
            .WithMany()
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        
        builder.HasKey(al => al.Id);
        
        builder.Property(al => al.Action)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(al => al.EntityName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(al => al.OldValues)
            .HasMaxLength(1000);
            
        builder.Property(al => al.NewValues)
            .HasMaxLength(1000);
            
        builder.Property(al => al.Description)
            .HasMaxLength(500);
            
        builder.Property(al => al.Username)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(al => al.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(al => al.IpAddress)
            .HasMaxLength(50);
            
        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
