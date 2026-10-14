using EMS_MVC.Services.Common;
using EMS_MVC.ViewModels.Activity;
using EMS_MVC.ViewModels.Attendance;
using EMS_MVC.ViewModels.Student;

namespace EMS_MVC.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ Đăng ký & Hủy đăng ký tham gia hoạt động
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Lê Thành Đạt
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Đăng ký tham gia hoạt động, kiểm tra các Business Rules BR01, BR02, BR03 trong transaction
    /// </summary>
    Task<ServiceResult> RegisterAsync(int activityId, int studentId);

    /// <summary>
    /// Hủy đăng ký tham gia hoạt động trước hạn chót cho phép
    /// </summary>
    Task<ServiceResult> CancelRegistrationAsync(int activityId, int studentId);

    /// <summary>
    /// Lấy danh sách các hoạt động sinh viên đã đăng ký
    /// </summary>
    Task<List<StudentActivityItemViewModel>> GetStudentRegistrationsAsync(int studentId);

    /// <summary>
    /// Lấy danh sách sinh viên đăng ký của một hoạt động cho Ban tổ chức
    /// </summary>
    Task<List<StudentRegistrationAttendanceItemViewModel>> GetActivityRegistrationsAsync(int activityId);
}
