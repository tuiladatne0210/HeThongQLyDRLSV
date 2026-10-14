using EMS_MVC.Services.Common;
using EMS_MVC.ViewModels.Student;

namespace EMS_MVC.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ Hồ sơ & Dashboard sinh viên (UC-004, UC-011, UC-012)
/// Trách nhiệm chính: Lê Thành Đạt; Review & phối hợp: Lê Thành Hy
/// </summary>
public interface IProfileService
{
    Task<StudentDashboardViewModel?> GetStudentDashboardAsync(int userId);
    Task<StudentProfileViewModel?> GetStudentProfileAsync(int userId);
    Task<ServiceResult> UpdateStudentProfileAsync(int userId, string? email, string? phoneNumber);
    Task<List<StudentActivityItemViewModel>> GetParticipationHistoryAsync(int userId);
    Task<List<PointHistoryItemViewModel>> GetPointHistoryAsync(int userId);
}

