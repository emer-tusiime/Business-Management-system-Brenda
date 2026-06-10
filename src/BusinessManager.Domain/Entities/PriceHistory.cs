using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class PriceHistory
{
    public int Id { get; set; }
    
    public decimal OldPrice { get; set; }
    
    public decimal NewPrice { get; set; }
    
    public string PriceType { get; set; } = string.Empty; // "Service" or "Product"
    
    public int ItemId { get; set; } // ServiceItem.Id or Product.Id
    
    public int UserId { get; set; }
    
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ServiceItem? ServiceItem { get; set; }
    public virtual Product? Product { get; set; }
}
