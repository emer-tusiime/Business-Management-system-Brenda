using System;

namespace BusinessManager.Domain.Entities;

public class Saving
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Recipient { get; set; } = "BANK";
    public string? Notes { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual User User { get; set; } = null!;
}
