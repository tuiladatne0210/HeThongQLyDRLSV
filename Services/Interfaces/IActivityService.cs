using EMS_MVC.Services.Common;
using EMS_MVC.ViewModels.Activity;
using EMS_MVC.ViewModels.Manager;

namespace EMS_MVC.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ Quản lý Hoạt động & Quy trình vòng đời phê duyệt
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Lê Thành Đạt
/// </summary>
public interface IActivityService
{
    // Dành cho Sinh viên / Khách
    Task<ActivityListViewModel> GetStudentActivitiesAsync(string? search, int? typeId, int? semesterId, int? studentId);
    Task<ActivityDetailViewModel?> GetActivityDetailAsync(int activityId, int? studentId);

    // Dành cho Ban tổ chức (Organizer)
    Task<List<ActivityCardItemViewModel>> GetOrganizerActivitiesAsync(int organizerUserId);
    Task<ActivityCreateEditViewModel?> GetActivityForEditAsync(int activityId, int organizerUserId);
    Task<ServiceResult<int>> CreateActivityAsync(ActivityCreateEditViewModel model, int organizerUserId);
    Task<ServiceResult> UpdateActivityAsync(ActivityCreateEditViewModel model, int organizerUserId);
    Task<ServiceResult> SubmitForApprovalAsync(int activityId, int organizerUserId);
    Task<ServiceResult> OpenRegistrationAsync(int activityId, int organizerUserId);

    // Dành cho Quản lý (Manager)
    Task<ActivityApprovalQueueViewModel> GetApprovalQueueAsync();
    Task<ServiceResult> ApproveActivityAsync(int activityId, int managerUserId, string? reviewNote);
    Task<ServiceResult> RejectActivityAsync(int activityId, int managerUserId, string reviewNote);
}
