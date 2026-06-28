using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class DebtPayment
{
    public int Id { get; set; }

    public int DebtorId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Debtor Debtor { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
