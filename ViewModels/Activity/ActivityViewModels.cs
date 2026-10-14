using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.ViewModels.Activity;

/// <summary>
/// ViewModel tra cứu danh sách hoạt động (Màn hình 05 Stitch)
/// </summary>
public class ActivityListViewModel
{
    public string? SearchKeyword { get; set; }
    public int? ActivityTypeId { get; set; }
    public int? SemesterId { get; set; }
    public string? StatusFilter { get; set; }

    public List<ActivityCardItemViewModel> Activities { get; set; } = new();
}

public class ActivityCardItemViewModel
{
    public int ActivityId { get; set; }
    public string? ActivityCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ActivityTypeName { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime RegistrationStartAt { get; set; }
    public DateTime RegistrationEndAt { get; set; }
    public int? MaxParticipants { get; set; }
    public int CurrentParticipantsCount { get; set; }
    public int Points { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StudentRegistrationStatus { get; set; }
}

/// <summary>
/// ViewModel chi tiết hoạt động (Màn hình 06 Stitch)
/// </summary>
public class ActivityDetailViewModel
{
    public int ActivityId { get; set; }
    public string? ActivityCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ActivityTypeName { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime RegistrationStartAt { get; set; }
    public DateTime RegistrationEndAt { get; set; }
    public int? MaxParticipants { get; set; }
    public int CurrentParticipantsCount { get; set; }
    public int Points { get; set; }
    public string AttendanceMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }

    // Trạng thái của sinh viên hiện tại đối với hoạt động này
    public bool IsStudent { get; set; }
    public bool IsRegistered { get; set; }
    public string? RegistrationStatus { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public bool CanRegister { get; set; }
    public bool CanCancel { get; set; }
    public string? CannotRegisterReason { get; set; }
}

/// <summary>
/// ViewModel tạo hoặc chỉnh sửa hoạt động (Organizer - Màn hình 15 Stitch)
/// </summary>
public class ActivityCreateEditViewModel
{
    public int? ActivityId { get; set; }

    [Display(Name = "Mã hoạt động")]
    [MaxLength(30)]
    public string? ActivityCode { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên hoạt động")]
    [StringLength(200, ErrorMessage = "Tên hoạt động tối đa 200 ký tự")]
    [Display(Name = "Tên hoạt động")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại hoạt động")]
    [Display(Name = "Loại hoạt động")]
    public int ActivityTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn học kỳ")]
    [Display(Name = "Học kỳ")]
    public int SemesterId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa điểm")]
    [StringLength(300, ErrorMessage = "Địa điểm tối đa 300 ký tự")]
    [Display(Name = "Địa điểm tổ chức")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
    [Display(Name = "Thời gian bắt đầu hoạt động")]
    public DateTime StartAt { get; set; } = DateTime.UtcNow.AddDays(7);

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
    [Display(Name = "Thời gian kết thúc hoạt động")]
    public DateTime EndAt { get; set; } = DateTime.UtcNow.AddDays(7).AddHours(3);

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu mở đăng ký")]
    [Display(Name = "Bắt đầu nhận đăng ký")]
    public DateTime RegistrationStartAt { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc đăng ký")]
    [Display(Name = "Hạn chót đăng ký")]
    public DateTime RegistrationEndAt { get; set; } = DateTime.UtcNow.AddDays(5);

    [Display(Name = "Số lượng tham gia tối đa")]
    [Range(1, 10000, ErrorMessage = "Số lượng tối đa phải lớn hơn 0")]
    public int? MaxParticipants { get; set; }

    [Display(Name = "Điểm rèn luyện")]
    [Range(0, 100, ErrorMessage = "Điểm phải từ 0 trở lên")]
    public int? PointOverride { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chế độ điểm danh")]
    [Display(Name = "Chế độ điểm danh")]
    public string AttendanceMode { get; set; } = "CHECKIN_ONLY";

    public string Status { get; set; } = "DRAFT";
    public string? ReviewNote { get; set; }
}
