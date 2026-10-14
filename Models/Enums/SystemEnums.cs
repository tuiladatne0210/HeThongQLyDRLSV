namespace EMS_MVC.Models.Enums;

/// <summary>
/// Trạng thái tài khoản người dùng
/// </summary>
public static class UserStatus
{
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Active = "ACTIVE";
    public const string Rejected = "REJECTED";
    public const string Disabled = "DISABLED";
}

/// <summary>
/// Các vai trò hệ thống
/// </summary>
public static class SystemRoles
{
    public const string Student = "Student";
    public const string Organizer = "Organizer";
    public const string Manager = "Manager";
    public const string Admin = "Admin";
}

/// <summary>
/// Trạng thái vòng đời hoạt động
/// </summary>
public static class ActivityStatus
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Approved = "APPROVED";
    public const string OpenRegistration = "OPEN_REGISTRATION";
    public const string RegistrationClosed = "REGISTRATION_CLOSED";
    public const string Ongoing = "ONGOING";
    public const string Completed = "COMPLETED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";
}

/// <summary>
/// Chế độ điểm danh hoạt động
/// </summary>
public static class AttendanceMode
{
    public const string CheckInOnly = "CHECKIN_ONLY";
    public const string CheckInCheckOut = "CHECKIN_CHECKOUT";
}

/// <summary>
/// Trạng thái đăng ký tham gia hoạt động
/// </summary>
public static class RegistrationStatus
{
    public const string Registered = "REGISTERED";
    public const string Attended = "ATTENDED";
    public const string Absent = "ABSENT";
    public const string Cancelled = "CANCELLED";
}

/// <summary>
/// Trạng thái phiên điểm danh
/// </summary>
public static class SessionStatus
{
    public const string Open = "OPEN";
    public const string Closed = "CLOSED";
}

/// <summary>
/// Phương thức ghi nhận điểm danh
/// </summary>
public static class AttendanceMethod
{
    public const string Qr = "QR";
    public const string Manual = "MANUAL";
}

/// <summary>
/// Trạng thái bản ghi điểm danh
/// </summary>
public static class AttendanceRecordStatus
{
    public const string Valid = "VALID";
    public const string Revoked = "REVOKED";
}

/// <summary>
/// Loại quy tắc điểm rèn luyện
/// </summary>
public static class PointRuleType
{
    public const string Participation = "PARTICIPATION";
    public const string Bonus = "BONUS";
    public const string Penalty = "PENALTY";
}

/// <summary>
/// Loại giao dịch điểm trong sổ cái append-only
/// </summary>
public static class TransactionType
{
    public const string ActivityReward = "ACTIVITY_REWARD";
    public const string Adjustment = "ADJUSTMENT";
    public const string Bonus = "BONUS";
    public const string Penalty = "PENALTY";
    public const string Reversal = "REVERSAL";
}

/// <summary>
/// Trạng thái yêu cầu điều chỉnh điểm
/// </summary>
public static class AdjustmentStatus
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}

/// <summary>
/// Quyết định phê duyệt yêu cầu điều chỉnh điểm
/// </summary>
public static class ApprovalDecision
{
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}

/// <summary>
/// Loại điều kiện đạt danh hiệu / huy hiệu
/// </summary>
public static class AchievementConditionType
{
    public const string TotalPoints = "TOTAL_POINTS";
    public const string ActivityCount = "ACTIVITY_COUNT";
    public const string ActivityTypeCount = "ACTIVITY_TYPE_COUNT";
}

/// <summary>
/// Nguồn cấp thành tích
/// </summary>
public static class GrantSource
{
    public const string Automatic = "AUTOMATIC";
    public const string Manual = "MANUAL";
}
