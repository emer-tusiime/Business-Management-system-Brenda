using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class Sale
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ReceiptNumber { get; set; } = string.Empty;
    
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    
    public decimal Subtotal { get; set; }
    
    public decimal TaxAmount { get; set; }
    
    public decimal DiscountAmount { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public decimal AmountPaid { get; set; }
    
    public decimal ChangeAmount { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [MaxLength(100)]
    public string? CustomerName { get; set; }
    
    [MaxLength(20)]
    public string? CustomerPhone { get; set; }
    
    public int UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
