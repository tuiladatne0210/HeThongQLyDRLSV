using EMS_MVC.Services.Common;
using EMS_MVC.ViewModels.Attendance;

namespace EMS_MVC.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ Điểm danh (Phiên điểm danh và Điểm danh thủ công BR04)
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Ngô Trọng Thiện, Lê Thành Đạt
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Lấy danh sách các phiên điểm danh của hoạt động
    /// </summary>
    Task<ActivitySessionsViewModel?> GetActivitySessionsAsync(int activityId);

    /// <summary>
    /// Tạo phiên điểm danh mới cho hoạt động
    /// </summary>
    Task<ServiceResult<int>> CreateSessionAsync(SessionCreateViewModel model, int organizerUserId);

    /// <summary>
    /// Đóng phiên điểm danh
    /// </summary>
    Task<ServiceResult> CloseSessionAsync(int sessionId, int organizerUserId);

    /// <summary>
    /// Lấy danh sách sinh viên đăng ký để Ban tổ chức thực hiện điểm danh thủ công
    /// </summary>
    Task<ManualAttendanceViewModel?> GetManualAttendanceViewModelAsync(int sessionId, string? keyword);

    /// <summary>
    /// Ghi nhận điểm danh thủ công cho sinh viên có đăng ký hợp lệ (BR04)
    /// </summary>
    Task<ServiceResult> CheckInManualAsync(int sessionId, int registrationId, int recordedByUserId);
}
