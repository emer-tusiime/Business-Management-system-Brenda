using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Infrastructure.Data.Configurations;

public class SeedData : IEntityTypeConfiguration<User>, 
    IEntityTypeConfiguration<ServiceItem>,
    IEntityTypeConfiguration<Product>,
    IEntityTypeConfiguration<ExpenseCategory>,
    IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Default Admin User
        builder.HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "O2Esdae1BIpDX7bsgeUv+S1teVqLWpwXBw9qY8l6U7I=", // SHA256 of Admin123
                FullName = "System Administrator",
                Email = "admin@businessmanager.com",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }

    public void Configure(EntityTypeBuilder<ServiceItem> builder)
    {
        builder.HasData(
            // Photocopy Services
            new ServiceItem { Id = 1, Name = "Photocopy Standard", Category = ServiceCategory.Photocopy, DefaultPrice = 200, IsFlexiblePrice = false, IsActive = true },
            new ServiceItem { Id = 2, Name = "Photocopy Premium", Category = ServiceCategory.Photocopy, DefaultPrice = 500, IsFlexiblePrice = false, IsActive = true },
            
            // Printing Services
            new ServiceItem { Id = 3, Name = "Printing Black", Category = ServiceCategory.Printing, DefaultPrice = 500, IsFlexiblePrice = false, IsActive = true },
            new ServiceItem { Id = 4, Name = "Printing Color", Category = ServiceCategory.Printing, DefaultPrice = 1000, IsFlexiblePrice = false, IsActive = true },
            
            // Typing Service
            new ServiceItem { Id = 5, Name = "Typing", Category = ServiceCategory.Typing, DefaultPrice = 1000, IsFlexiblePrice = true, IsActive = true },
            
            // Binding Service
            new ServiceItem { Id = 6, Name = "Binding", Category = ServiceCategory.Binding, DefaultPrice = 1500, IsFlexiblePrice = true, IsActive = true },
            
            // Sealing Service
            new ServiceItem { Id = 7, Name = "Sealing", Category = ServiceCategory.Sealing, DefaultPrice = 1500, IsFlexiblePrice = false, IsActive = true },
            
            // Labelling Service
            new ServiceItem { Id = 8, Name = "Labelling", Category = ServiceCategory.Labelling, DefaultPrice = 5000, IsFlexiblePrice = true, IsActive = true },
            
            // Email Creation Service
            new ServiceItem { Id = 9, Name = "Email Creation", Category = ServiceCategory.EmailCreation, DefaultPrice = 2000, IsFlexiblePrice = true, IsActive = true },
            
            // Passport Application Service
            new ServiceItem { Id = 10, Name = "Passport Application", Category = ServiceCategory.PassportApplication, DefaultPrice = 5000, IsFlexiblePrice = true, IsActive = true },
            
            // Branding Service
            new ServiceItem { Id = 11, Name = "Branding", Category = ServiceCategory.Branding, DefaultPrice = 10000, IsFlexiblePrice = true, IsActive = true },
            
            // Scanning Service
            new ServiceItem { Id = 12, Name = "Scanning", Category = ServiceCategory.Scanning, DefaultPrice = 500, IsFlexiblePrice = true, IsActive = true },
            
            // Laminating Service
            new ServiceItem { Id = 13, Name = "Laminating", Category = ServiceCategory.Laminating, DefaultPrice = 1000, IsFlexiblePrice = true, IsActive = true }
        );
    }

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasData(
            // Water Products
            new Product { Id = 1, Name = "Water Ice Big", SKU = "WIB001", BuyingPrice = 500, SellingPrice = 1000, CurrentStock = 50, ReorderLevel = 10, MaxStock = 100, Unit = "bottles" },
            new Product { Id = 2, Name = "Water Ice Small", SKU = "WIS001", BuyingPrice = 250, SellingPrice = 500, CurrentStock = 30, ReorderLevel = 10, MaxStock = 80, Unit = "bottles" },
            new Product { Id = 3, Name = "Rwenzori Water Big", SKU = "RWB001", BuyingPrice = 1200, SellingPrice = 2000, CurrentStock = 25, ReorderLevel = 8, MaxStock = 60, Unit = "bottles" },
            new Product { Id = 4, Name = "Rwenzori Water Small", SKU = "RWS001", BuyingPrice = 600, SellingPrice = 1000, CurrentStock = 40, ReorderLevel = 10, MaxStock = 80, Unit = "bottles" },
            
            // Soda Products
            new Product { Id = 5, Name = "Soda", SKU = "SOD001", BuyingPrice = 600, SellingPrice = 1000, CurrentStock = 35, ReorderLevel = 10, MaxStock = 70, Unit = "bottles" },
            
            // Stationery Products
            new Product { Id = 6, Name = "A4 Paper", SKU = "PAP001", BuyingPrice = 8000, SellingPrice = 12000, CurrentStock = 20, ReorderLevel = 5, MaxStock = 50, Unit = "reams" },
            new Product { Id = 7, Name = "Black Ink", SKU = "INK001", BuyingPrice = 15000, SellingPrice = 25000, CurrentStock = 10, ReorderLevel = 3, MaxStock = 20, Unit = "cartridges" },
            new Product { Id = 8, Name = "Color Ink", SKU = "INK002", BuyingPrice = 20000, SellingPrice = 35000, CurrentStock = 8, ReorderLevel = 2, MaxStock = 15, Unit = "cartridges" },
            new Product { Id = 9, Name = "Binding Comb", SKU = "BIN001", BuyingPrice = 500, SellingPrice = 1000, CurrentStock = 100, ReorderLevel = 20, MaxStock = 200, Unit = "pieces" },
            new Product { Id = 10, Name = "Label Sticker", SKU = "LAB001", BuyingPrice = 2000, SellingPrice = 3500, CurrentStock = 50, ReorderLevel = 10, MaxStock = 100, Unit = "sheets" }
        );
    }

    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.HasData(
            new ExpenseCategory { Id = 1, Name = "Utilities", Description = "Electricity, water, internet bills", Color = "#3498db" },
            new ExpenseCategory { Id = 2, Name = "Rent", Description = "Shop/office rent", Color = "#e74c3c" },
            new ExpenseCategory { Id = 3, Name = "Salaries", Description = "Staff salaries and wages", Color = "#2ecc71" },
            new ExpenseCategory { Id = 4, Name = "Supplies", Description = "Office supplies and materials", Color = "#f39c12" },
            new ExpenseCategory { Id = 5, Name = "Maintenance", Description = "Equipment maintenance and repairs", Color = "#9b59b6" },
            new ExpenseCategory { Id = 6, Name = "Marketing", Description = "Advertising and promotion", Color = "#1abc9c" },
            new ExpenseCategory { Id = 7, Name = "Transport", Description = "Transportation and fuel", Color = "#34495e" },
            new ExpenseCategory { Id = 8, Name = "Other", Description = "Miscellaneous expenses", Color = "#95a5a6" }
        );
    }

    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.HasData(
            new Setting { Id = 1, Key = "BusinessName", Value = "Alinda Brenda", Description = "Business name for receipts and reports", Category = "Business" },
            new Setting { Id = 11, Key = "DrawerOpeningBalance", Value = "0", Description = "Cash in drawer at start of the day", Category = "Financial" },
            new Setting { Id = 2, Key = "BusinessAddress", Value = "123 Main Street, Kampala, Uganda", Description = "Business address", Category = "Business" },
            new Setting { Id = 3, Key = "BusinessPhone", Value = "+256 123 456 789", Description = "Business phone number", Category = "Business" },
            new Setting { Id = 4, Key = "BusinessEmail", Value = "info@businessmanager.com", Description = "Business email address", Category = "Business" },
            new Setting { Id = 5, Key = "Currency", Value = "UGX", Description = "Currency symbol", Category = "Business" },
            new Setting { Id = 6, Key = "ReceiptFooter", Value = "Thank you for your business!", Description = "Footer text for receipts", Category = "Receipt" },
            new Setting { Id = 7, Key = "TaxRate", Value = "0.18", Description = "Tax rate (as decimal)", Category = "Financial" },
            new Setting { Id = 8, Key = "BackupPath", Value = "C:\\BusinessManager\\Backups", Description = "Default backup folder path", Category = "System" },
            new Setting { Id = 9, Key = "LowStockAlert", Value = "true", Description = "Enable low stock alerts", Category = "Inventory" },
            new Setting { Id = 10, Key = "Theme", Value = "Light", Description = "Application theme", Category = "Appearance" }
        );
    }
}
