using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.Models.Entities;

/// <summary>
/// Vai trò người dùng (dbo.Roles)
/// </summary>
public class Role
{
    public int RoleId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>
/// Gán vai trò cho tài khoản (dbo.UserRoles)
/// </summary>
public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public int? AssignedByUserId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
    public virtual User? AssignedByUser { get; set; }
}
