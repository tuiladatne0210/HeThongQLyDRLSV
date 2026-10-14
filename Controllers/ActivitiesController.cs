using System.Security.Claims;
using EMS_MVC.Data;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Controllers;

/// <summary>
/// Điều phối danh mục Hoạt động ngoại khóa và Đăng ký tham gia cho Sinh viên
/// Trách nhiệm chính: Lê Thành Đạt (UI); Lê Thành Hy (Nghiệp vụ Đăng ký)
/// </summary>
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IRegistrationService _registrationService;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public ActivitiesController(
        IActivityService activityService,
        IRegistrationService registrationService,
        IAuthService authService,
        ApplicationDbContext context)
    {
        _activityService = activityService;
        _registrationService = registrationService;
        _authService = authService;
        _context = context;
    }

    /// <summary>
    /// Màn hình 05 Stitch: Danh sách tra cứu hoạt động ngoại khóa
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, int? typeId, int? semesterId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        var model = await _activityService.GetStudentActivitiesAsync(search, typeId, semesterId, studentId);

        await LoadFilterDropdownsAsync();
        return View(model);
    }

    /// <summary>
    /// Màn hình 06 Stitch: Chi tiết hoạt động và trạng thái đăng ký của sinh viên
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        var model = await _activityService.GetActivityDetailAsync(id, studentId);

        if (model == null)
        {
            return NotFound("Không tìm thấy hoạt động yêu cầu.");
        }

        return View(model);
    }

    /// <summary>
    /// Sinh viên thực hiện đăng ký tham gia hoạt động
    /// Áp dụng các quy tắc BR01 (chống trùng), BR02 (đúng thời hạn), BR03 (đúng sức chứa)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = SystemRoles.Student)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int activityId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (!studentId.HasValue)
        {
            TempData["ErrorMessage"] = "Bạn chưa có hồ sơ sinh viên hợp lệ để đăng ký hoạt động.";
            return RedirectToAction(nameof(Details), new { id = activityId });
        }

        var result = await _registrationService.RegisterAsync(activityId, studentId.Value);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = activityId });
    }

    /// <summary>
    /// Sinh viên hủy đăng ký hoạt động trước hạn chót
    /// </summary>
    [HttpPost]
    [Authorize(Roles = SystemRoles.Student)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int activityId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (!studentId.HasValue)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ sinh viên.";
            return RedirectToAction(nameof(Details), new { id = activityId });
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

        return RedirectToAction(nameof(Details), new { id = activityId });
    }

    private async Task<int?> GetCurrentStudentIdAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return null;

        var claim = User.FindFirstValue("StudentId");
        if (int.TryParse(claim, out var sId)) return sId;

        var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userClaim, out var uId))
        {
            return await _authService.GetStudentIdByUserIdAsync(uId);
        }

        return null;
    }

    private async Task LoadFilterDropdownsAsync()
    {
        ViewBag.ActivityTypes = new SelectList(
            await _context.ActivityTypes.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(),
            "ActivityTypeId", "Name");

        ViewBag.Semesters = new SelectList(
            await _context.Semesters.OrderByDescending(s => s.StartDate).ToListAsync(),
            "SemesterId", "Name");
    }
}
