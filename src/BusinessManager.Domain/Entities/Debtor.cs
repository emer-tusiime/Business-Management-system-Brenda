using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class Debtor
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public DateTime RecordDate { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int UserId { get; set; }

    public bool IsSettled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public decimal Balance => TotalAmount - AmountPaid;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<DebtPayment> Payments { get; set; } = new List<DebtPayment>();
}
