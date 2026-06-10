using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessManager.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;
    
    public int? EntityId { get; set; }
    
    [MaxLength(1000)]
    public string? OldValues { get; set; }
    
    [MaxLength(1000)]
    public string? NewValues { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public int UserId { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
