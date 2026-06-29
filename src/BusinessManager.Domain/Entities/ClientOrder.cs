using System;
using System.ComponentModel.DataAnnotations;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Domain.Entities;

public class ClientOrder
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string ClientName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime PickupDate { get; set; } = DateTime.Today.AddDays(1);

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public bool IsOverdue => Status != OrderStatus.Delivered && PickupDate.Date < DateTime.Today;
    public bool IsDueToday => Status != OrderStatus.Delivered && PickupDate.Date == DateTime.Today;
}
