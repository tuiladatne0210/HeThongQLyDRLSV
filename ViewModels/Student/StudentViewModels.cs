namespace EMS_MVC.ViewModels.Student;

/// <summary>
/// ViewModel Dashboard Sinh viên (theo thiết kế màn hình 04 Stitch)
/// </summary>
public class StudentDashboardViewModel
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;

    // KPI thống kê
    public long TotalPoints { get; set; }
    public int AttendedActivitiesCount { get; set; }
    public int RegisteredUpcomingCount { get; set; }
    public string Classification { get; set; } = "Chưa xếp loại";

    // Danh sách hoạt động sắp diễn ra đã đăng ký
    public List<StudentActivityItemViewModel> UpcomingActivities { get; set; } = new();

    // Danh sách hoạt động mới đang mở đăng ký
    public List<StudentActivityItemViewModel> OpenRegistrationActivities { get; set; } = new();
}

public class StudentActivityItemViewModel
{
    public int ActivityId { get; set; }
    public string? ActivityCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ActivityTypeName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime RegistrationEndAt { get; set; }
    public int Points { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RegistrationStatus { get; set; }
    public int? RegisteredCount { get; set; }
    public int? MaxParticipants { get; set; }
}

/// <summary>
/// ViewModel Hồ sơ cá nhân sinh viên
/// </summary>
public class StudentProfileViewModel
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// ViewModel Lịch sử tích lũy điểm rèn luyện (UC-012)
/// </summary>
public class PointHistoryItemViewModel
{
    public long TransactionId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public string ActivityTypeName { get; set; } = string.Empty;
    public int Points { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

