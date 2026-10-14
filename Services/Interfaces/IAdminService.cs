using EMS_MVC.Services.Common;
using EMS_MVC.ViewModels.Admin;

namespace EMS_MVC.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ Quản trị hệ thống & Phân quyền người dùng
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Lấy danh sách người dùng kèm bộ lọc trạng thái, vai trò và từ khóa tìm kiếm
    /// </summary>
    Task<UserManagementViewModel> GetUsersAsync(string? statusFilter, string? roleFilter, string? keyword);

    /// <summary>
    /// Lấy thông tin tài khoản cần kích hoạt hoặc phân quyền
    /// </summary>
    Task<ActivateUserViewModel?> GetUserForActivationAsync(int userId);

    /// <summary>
    /// Kích hoạt tài khoản, cập nhật trạng thái và gán vai trò người dùng
    /// </summary>
    Task<ServiceResult> UpdateUserStatusAndRolesAsync(ActivateUserViewModel model, int adminUserId);
}
