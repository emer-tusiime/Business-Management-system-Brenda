using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Domain.Entities;

public class ServiceItem
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public ServiceCategory Category { get; set; }
    
    public decimal DefaultPrice { get; set; }
    
    public bool IsFlexiblePrice { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public virtual ICollection<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();
}
