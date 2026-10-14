using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.ViewModels.Admin;

/// <summary>
/// Quản lý người dùng và phân quyền (Màn hình 29 Stitch)
/// </summary>
public class UserManagementViewModel
{
    public string? StatusFilter { get; set; }
    public string? RoleFilter { get; set; }
    public string? SearchKeyword { get; set; }

    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int PendingUsers { get; set; }
    public int DisabledUsers { get; set; }

    public List<UserListItemViewModel> Users { get; set; } = new();
}

public class UserListItemViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string? StudentCode { get; set; }
    public string? ClassName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Kích hoạt tài khoản và gán vai trò
/// </summary>
public class ActivateUserViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? StudentCode { get; set; }
    public string? ClassName { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Trạng thái mới")]
    public string NewStatus { get; set; } = "ACTIVE"; // ACTIVE, REJECTED, DISABLED

    [Display(Name = "Các vai trò được cấp")]
    public List<string> SelectedRoles { get; set; } = new();

    public List<string> AvailableRoles { get; set; } = new();
}
