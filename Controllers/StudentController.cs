using System.Security.Claims;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS_MVC.Controllers;

/// <summary>
/// Điều phối Cổng Sinh viên (Dashboard, Lịch sử tham gia, Hồ sơ cá nhân)
/// Trách nhiệm chính: Lê Thành Đạt; Review & phối hợp: Lê Thành Hy
/// </summary>
[Authorize(Roles = SystemRoles.Student)]
public class StudentController : Controller
{
    private readonly IProfileService _profileService;
    private readonly IRegistrationService _registrationService;
    private readonly IAuthService _authService;

    public StudentController(
        IProfileService profileService,
        IRegistrationService registrationService,
        IAuthService authService)
    {
        _profileService = profileService;
        _registrationService = registrationService;
        _authService = authService;
    }

    /// <summary>
    /// Màn hình 04 Stitch: Dashboard sinh viên (KPI điểm, hoạt động tham gia, danh sách sắp diễn ra)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var userId = GetCurrentUserId();
        var model = await _profileService.GetStudentDashboardAsync(userId);
        if (model == null)
        {
            return NotFound("Không tìm thấy hồ sơ sinh viên gắn với tài khoản hiện tại.");
        }

        return View(model);
    }

    /// <summary>
    /// Màn hình 07 Stitch: Danh sách hoạt động đã đăng ký của sinh viên
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MyActivities()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (!studentId.HasValue)
        {
            return NotFound("Không tìm thấy thông tin sinh viên.");
        }

        var list = await _registrationService.GetStudentRegistrationsAsync(studentId.Value);
        return View(list);
    }

    /// <summary>
    /// Xem thông tin hồ sơ cá nhân sinh viên (UC-004)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        var model = await _profileService.GetStudentProfileAsync(userId);
        if (model == null)
        {
            return NotFound("Không tìm thấy hồ sơ sinh viên.");
        }

        return View(model);
    }

    /// <summary>
    /// Cập nhật thông tin liên lạc cá nhân (SĐT, Email) - UC-004
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string? email, string? phoneNumber)
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.UpdateStudentProfileAsync(userId, email, phoneNumber);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }
        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Sinh viên hủy đăng ký hoạt động trước hạn chót - UC-008
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRegistration(int activityId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (!studentId.HasValue)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ sinh viên.";
            return RedirectToAction(nameof(MyActivities));
        }

        var result = await _registrationService.CancelRegistrationAsync(activityId, studentId.Value);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(MyActivities));
    }

    /// <summary>
    /// Màn hình Lịch sử tham gia hoạt động ngoại khóa - UC-011
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ParticipationHistory()
    {
        var userId = GetCurrentUserId();
        var list = await _profileService.GetParticipationHistoryAsync(userId);
        return View(list);
    }

    /// <summary>
    /// Màn hình Lịch sử tích lũy điểm rèn luyện - UC-012
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PointHistory()
    {
        var userId = GetCurrentUserId();
        var list = await _profileService.GetPointHistoryAsync(userId);
        return View(list);
    }


    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private async Task<int?> GetCurrentStudentIdAsync()
    {
        var claim = User.FindFirstValue("StudentId");
        if (int.TryParse(claim, out var sId)) return sId;

        var userId = GetCurrentUserId();
        return await _authService.GetStudentIdByUserIdAsync(userId);
    }
}
