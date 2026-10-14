using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.Models.Entities;

/// <summary>
/// Quy tắc điểm rèn luyện (dbo.PointRules)
/// </summary>
public class PointRule
{
    public int PointRuleId { get; set; }
    public int ActivityTypeId { get; set; }

    [Required]
    [MaxLength(150)]
    public string RuleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string RuleType { get; set; } = "PARTICIPATION";

    public int Points { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ActivityType ActivityType { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}

/// <summary>
/// Yêu cầu điều chỉnh điểm (dbo.PointAdjustments)
/// </summary>
public class PointAdjustment
{
    public int AdjustmentId { get; set; }
    public int StudentId { get; set; }
    public int? ActivityId { get; set; }
    public int RequestedDelta { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string EvidencePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    public int RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public long? PointsBefore { get; set; }
    public long? PointsAfter { get; set; }

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Activity? Activity { get; set; }
    public virtual User RequestedByUser { get; set; } = null!;
    public virtual ICollection<PointAdjustmentApproval> Approvals { get; set; } = new List<PointAdjustmentApproval>();
    public virtual PointTransaction? PointTransaction { get; set; }
}

/// <summary>
/// Lịch sử phê duyệt điều chỉnh điểm (dbo.PointAdjustmentApprovals)
/// </summary>
public class PointAdjustmentApproval
{
    public int ApprovalId { get; set; }
    public int AdjustmentId { get; set; }
    public int ApprovedByUserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Decision { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Comments { get; set; }

    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual PointAdjustment Adjustment { get; set; } = null!;
    public virtual User ApprovedByUser { get; set; } = null!;
}

/// <summary>
/// Sổ giao dịch điểm append-only (dbo.PointTransactions)
/// </summary>
public class PointTransaction
{
    public long TransactionId { get; set; }
    public int StudentId { get; set; }
    public int? ActivityId { get; set; }
    public int? PointRuleId { get; set; }
    public int? PointAdjustmentId { get; set; }
    public int Points { get; set; }

    [Required]
    [MaxLength(30)]
    public string TransactionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Activity? Activity { get; set; }
    public virtual PointRule? PointRule { get; set; }
    public virtual PointAdjustment? PointAdjustment { get; set; }
    public virtual User? CreatedByUser { get; set; }
}

/// <summary>
/// Danh hiệu / Huy hiệu (dbo.Achievements)
/// </summary>
public class Achievement
{
    public int AchievementId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? IconPath { get; set; }

    [Required]
    [MaxLength(40)]
    public string ConditionType { get; set; } = "TOTAL_POINTS";

    public int ThresholdValue { get; set; }
    public int? ActivityTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ActivityType? ActivityType { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<StudentAchievement> StudentAchievements { get; set; } = new List<StudentAchievement>();
}

/// <summary>
/// Thành tích đã cấp cho sinh viên (dbo.StudentAchievements)
/// </summary>
public class StudentAchievement
{
    public int StudentId { get; set; }
    public int AchievementId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(20)]
    public string GrantSource { get; set; } = "AUTOMATIC";

    public int? GrantedByUserId { get; set; }

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Achievement Achievement { get; set; } = null!;
    public virtual User? GrantedByUser { get; set; }
}

/// <summary>
/// Thông báo trong ứng dụng (dbo.Notifications)
/// </summary>
public class Notification
{
    public long NotificationId { get; set; }
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NotificationType { get; set; }

    [MaxLength(100)]
    public string? RelatedEntityType { get; set; }

    [MaxLength(100)]
    public string? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Nhật ký kiểm toán append-only (dbo.AuditLogs)
/// </summary>
public class AuditLog
{
    public long AuditLogId { get; set; }
    public int? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? User { get; set; }
}
