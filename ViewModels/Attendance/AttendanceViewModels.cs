using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.ViewModels.Attendance;

/// <summary>
/// Quản lý phiên điểm danh của hoạt động (Màn hình 18 Stitch)
/// </summary>
public class ActivitySessionsViewModel
{
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public string ActivityStatus { get; set; } = string.Empty;
    public string AttendanceMode { get; set; } = string.Empty;

    public List<AttendanceSessionItemViewModel> Sessions { get; set; } = new();
    public SessionCreateViewModel NewSession { get; set; } = new();
}

public class AttendanceSessionItemViewModel
{
    public int SessionId { get; set; }
    public string? SessionName { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttendeeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class SessionCreateViewModel
{
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phiên điểm danh")]
    [StringLength(150, ErrorMessage = "Tên phiên tối đa 150 ký tự")]
    [Display(Name = "Tên phiên điểm danh")]
    public string SessionName { get; set; } = "Điểm danh chính thức";

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
    [Display(Name = "Bắt đầu")]
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
    [Display(Name = "Kết thúc")]
    public DateTime EndsAt { get; set; } = DateTime.UtcNow.AddHours(2);
}

/// <summary>
/// Điểm danh thủ công (Màn hình 19 Stitch)
/// </summary>
public class ManualAttendanceViewModel
{
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public string SessionStatus { get; set; } = string.Empty;

    public string? SearchKeyword { get; set; }

    public List<StudentRegistrationAttendanceItemViewModel> Registrations { get; set; } = new();
}

public class StudentRegistrationAttendanceItemViewModel
{
    public int RegistrationId { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string RegistrationStatus { get; set; } = string.Empty;
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckInAt { get; set; }
    public string? Method { get; set; }
}
