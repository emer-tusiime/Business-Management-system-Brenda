using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class ExpenseCategory
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(20)]
    public string? Color { get; set; } = "#3498db";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
