using System.Security.Claims;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS_MVC.Controllers;

/// <summary>
/// Điều phối Quản lý Phiên điểm danh và Điểm danh thủ công (BR04)
/// Trách nhiệm chính: Lê Thành Hy (Nghiệp vụ); Lê Thành Đạt (UI Views)
/// </summary>
[Authorize(Roles = $"{SystemRoles.Organizer},{SystemRoles.Admin}")]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Màn hình 18 Stitch: Danh sách phiên điểm danh của hoạt động
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Sessions(int? activityId, int? id)
    {
        var targetActivityId = activityId ?? id ?? 0;
        var model = await _attendanceService.GetActivitySessionsAsync(targetActivityId);
        if (model == null)
        {
            return NotFound("Không tìm thấy hoạt động.");
        }

        return View(model);
    }

    /// <summary>
    /// Tạo phiên điểm danh mới
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession(SessionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin phiên điểm danh chưa hợp lệ.";
            return RedirectToAction(nameof(Sessions), new { activityId = model.ActivityId });
        }

        var userId = GetCurrentUserId();
        var result = await _attendanceService.CreateSessionAsync(model, userId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Sessions), new { activityId = model.ActivityId });
    }

    /// <summary>
    /// Đóng phiên điểm danh
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSession(int sessionId, int activityId)
    {
        var userId = GetCurrentUserId();
        var result = await _attendanceService.CloseSessionAsync(sessionId, userId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Sessions), new { activityId });
    }

    /// <summary>
    /// Màn hình 19 Stitch: Giao diện Điểm danh thủ công cho sinh viên có đăng ký hợp lệ
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Manual(int? sessionId, int? id, string? keyword)
    {
        var targetSessionId = sessionId ?? id ?? 0;
        var model = await _attendanceService.GetManualAttendanceViewModelAsync(targetSessionId, keyword);
        if (model == null)
        {
            return NotFound("Không tìm thấy phiên điểm danh.");
        }

        return View(model);
    }

    /// <summary>
    /// Ghi nhận điểm danh thủ công (Áp dụng Business Rule BR04)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckInManual(int sessionId, int registrationId)
    {
        var userId = GetCurrentUserId();
        var result = await _attendanceService.CheckInManualAsync(sessionId, registrationId, userId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Manual), new { sessionId });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
