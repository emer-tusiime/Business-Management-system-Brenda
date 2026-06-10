using System;
using System.ComponentModel.DataAnnotations;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Domain.Entities;

public class InventoryMovement
{
    public int Id { get; set; }
    
    public int ProductId { get; set; }
    
    public InventoryMovementType MovementType { get; set; }
    
    public int Quantity { get; set; }
    
    public decimal UnitCost { get; set; }
    
    public decimal TotalCost { get; set; }
    
    public int StockBefore { get; set; }
    
    public int StockAfter { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }
    
    public int UserId { get; set; }
    
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
