using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.Models.Entities;

/// <summary>
/// Loại hoạt động ngoại khóa (dbo.ActivityTypes)
/// </summary>
public class ActivityType
{
    public int ActivityTypeId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public virtual ICollection<PointRule> PointRules { get; set; } = new List<PointRule>();
}

/// <summary>
/// Hoạt động ngoại khóa / rèn luyện (dbo.Activities)
/// </summary>
public class Activity
{
    public int ActivityId { get; set; }

    [MaxLength(30)]
    public string? ActivityCode { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ActivityTypeId { get; set; }
    public int OrganizerId { get; set; }
    public int SemesterId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Location { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    public DateTime RegistrationStartAt { get; set; }
    public DateTime RegistrationEndAt { get; set; }

    public int? MaxParticipants { get; set; }
    public int? PointOverride { get; set; }

    [Required]
    [MaxLength(30)]
    public string AttendanceMode { get; set; } = "CHECKIN_ONLY";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "DRAFT";

    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    [MaxLength(1000)]
    public string? ReviewNote { get; set; }

    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ActivityType ActivityType { get; set; } = null!;
    public virtual User Organizer { get; set; } = null!;
    public virtual Semester Semester { get; set; } = null!;
    public virtual User? ReviewedByUser { get; set; }
    public virtual User? CancelledByUser { get; set; }

    public virtual ICollection<ActivityRegistration> Registrations { get; set; } = new List<ActivityRegistration>();
    public virtual ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}

/// <summary>
/// Đăng ký tham gia hoạt động (dbo.ActivityRegistrations)
/// </summary>
public class ActivityRegistration
{
    public int RegistrationId { get; set; }
    public int ActivityId { get; set; }
    public int StudentId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "REGISTERED";

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Activity Activity { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}

/// <summary>
/// Phiên điểm danh hoạt động (dbo.AttendanceSessions)
/// </summary>
public class AttendanceSession
{
    public int SessionId { get; set; }
    public int ActivityId { get; set; }

    [MaxLength(150)]
    public string? SessionName { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public byte[]? QrTokenHash { get; set; }
    public DateTime? QrExpiresAt { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "OPEN";

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public virtual Activity Activity { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}

/// <summary>
/// Bản ghi điểm danh sinh viên (dbo.Attendances)
/// </summary>
public class Attendance
{
    public int AttendanceId { get; set; }
    public int SessionId { get; set; }
    public int RegistrationId { get; set; }
    public int ActivityId { get; set; }

    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }

    [Required]
    [MaxLength(20)]
    public string Method { get; set; } = "MANUAL";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "VALID";

    public int RecordedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual AttendanceSession Session { get; set; } = null!;
    public virtual ActivityRegistration Registration { get; set; } = null!;
    public virtual Activity Activity { get; set; } = null!;
    public virtual User RecordedByUser { get; set; } = null!;
}
