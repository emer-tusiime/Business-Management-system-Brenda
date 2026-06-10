using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? PaidBy { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public int ExpenseCategoryId { get; set; }
    
    public int UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ExpenseCategory ExpenseCategory { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
