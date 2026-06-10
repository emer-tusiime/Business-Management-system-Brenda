using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? SKU { get; set; }
    
    public decimal BuyingPrice { get; set; }
    
    public decimal CostPrice { get; set; }
    
    public decimal SellingPrice { get; set; }
    
    public int CurrentStock { get; set; }
    
    public int ReorderLevel { get; set; }
    
    public int MaxStock { get; set; }
    
    [MaxLength(255)]
    public string? Supplier { get; set; }
    
    [MaxLength(100)]
    public string? Unit { get; set; } = "pcs";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    public virtual ICollection<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();
}
