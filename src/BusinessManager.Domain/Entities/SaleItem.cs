using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class SaleItem
{
    public int Id { get; set; }
    
    public int SaleId { get; set; }
    
    public int Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    public decimal TotalPrice { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Service item reference (nullable)
    public int? ServiceItemId { get; set; }
    
    // Product reference (nullable)
    public int? ProductId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Sale Sale { get; set; } = null!;
    public virtual ServiceItem? ServiceItem { get; set; }
    public virtual Product? Product { get; set; }
}
