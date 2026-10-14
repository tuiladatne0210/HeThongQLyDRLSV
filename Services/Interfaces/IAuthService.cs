using System.Security.Claims;
using EMS_MVC.Services.Common;
using EMS_MVC.ViewModels.Account;

namespace EMS_MVC.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ Xác thực & Quản lý phiên tài khoản
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Ngô Trọng Thiện
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng ký tài khoản sinh viên mới, mặc định trạng thái PENDING_APPROVAL
    /// </summary>
    Task<ServiceResult> RegisterStudentAsync(RegisterViewModel model);

    /// <summary>
    /// Xác thực đăng nhập bằng Cookie, kiểm tra mật khẩu đã băm và trạng thái ACTIVE
    /// </summary>
    Task<ServiceResult<ClaimsPrincipal>> AuthenticateUserAsync(LoginViewModel model);

    /// <summary>
    /// Đổi mật khẩu tài khoản
    /// </summary>
    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordViewModel model);

    /// <summary>
    /// Lấy danh sách tên các Role của người dùng
    /// </summary>
    Task<List<string>> GetUserRolesAsync(int userId);

    /// <summary>
    /// Lấy StudentId từ UserId nếu có
    /// </summary>
    Task<int?> GetStudentIdByUserIdAsync(int userId);
}
