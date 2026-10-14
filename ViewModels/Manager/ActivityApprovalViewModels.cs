using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.ViewModels.Manager;

/// <summary>
/// Hàng đợi phê duyệt hoạt động (Màn hình 23 Stitch)
/// </summary>
public class ActivityApprovalQueueViewModel
{
    public List<ActivityApprovalItemViewModel> PendingActivities { get; set; } = new();
    public List<ActivityApprovalItemViewModel> RecentReviewedActivities { get; set; } = new();
}

public class ActivityApprovalItemViewModel
{
    public int ActivityId { get; set; }
    public string? ActivityCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ActivityTypeName { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int? MaxParticipants { get; set; }
    public int Points { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>
/// Quyết định phê duyệt / từ chối hoạt động
/// </summary>
public class ActivityReviewDecisionViewModel
{
    public int ActivityId { get; set; }
    public string Action { get; set; } = "APPROVE"; // "APPROVE" hoặc "REJECT"

    [Display(Name = "Ý kiến / Lý do từ chối")]
    [StringLength(1000, ErrorMessage = "Ý kiến tối đa 1000 ký tự")]
    public string? ReviewNote { get; set; }
}

/// <summary>
/// Báo cáo thống kê dành cho Cán bộ Quản lý (UC-028)
/// </summary>
public class ManagerReportViewModel
{
    public int TotalActivities { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int OpenRegistrationCount { get; set; }
    public int TotalParticipantsRegistered { get; set; }
    public int TotalParticipantsAttended { get; set; }
    public long TotalPointsAwarded { get; set; }
    public List<ActivityTypeStatViewModel> TypeStats { get; set; } = new();
    public List<ActivityApprovalItemViewModel> RecentActivities { get; set; } = new();
}

public class ActivityTypeStatViewModel
{
    public string TypeName { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
    public int ParticipantCount { get; set; }
}

