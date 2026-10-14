using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS_MVC.Models.Entities;

/// <summary>
/// Tài khoản người dùng hệ thống (dbo.Users)
/// </summary>
public class User
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NormalizedUsername { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(256)]
    public string? NormalizedEmail { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "PENDING_APPROVAL";

    public bool EmailConfirmed { get; set; }

    public int AccessFailedCount { get; set; }

    public DateTime? LockoutEndAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    // Navigation properties
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual Student? StudentProfile { get; set; }
    public virtual ICollection<Activity> OrganizedActivities { get; set; } = new List<Activity>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
