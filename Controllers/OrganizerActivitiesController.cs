using System.Security.Claims;
using EMS_MVC.Data;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Controllers;

/// <summary>
/// Điều phối Cổng Ban tổ chức (Tạo, sửa, gửi duyệt, mở đăng ký, quản lý danh sách đăng ký)
/// Trách nhiệm chính: Lê Thành Hy (Nghiệp vụ); Lê Thành Đạt (UI Views)
/// </summary>
[Authorize(Roles = $"{SystemRoles.Organizer},{SystemRoles.Admin}")]
public class OrganizerActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IRegistrationService _registrationService;
    private readonly ApplicationDbContext _context;

    public OrganizerActivitiesController(
        IActivityService activityService,
        IRegistrationService registrationService,
        ApplicationDbContext context)
    {
        _activityService = activityService;
        _registrationService = registrationService;
        _context = context;
    }

    /// <summary>
    /// Màn hình 14 Stitch: Danh sách hoạt động do Ban tổ chức quản lý
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var organizerId = GetCurrentUserId();
        var activities = await _activityService.GetOrganizerActivitiesAsync(organizerId);
        return View(activities);
    }

    /// <summary>
    /// Màn hình 15 Stitch: Tạo hoạt động mới
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new ActivityCreateEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        var organizerId = GetCurrentUserId();
        var result = await _activityService.CreateActivityAsync(model, organizerId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await LoadDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    /// <summary>
    /// Chỉnh sửa hoạt động (chỉ cho phép khi ở DRAFT hoặc REJECTED)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var organizerId = GetCurrentUserId();
        var model = await _activityService.GetActivityForEditAsync(id, organizerId);
        if (model == null)
        {
            return NotFound("Không tìm thấy hoạt động hoặc bạn không có quyền sửa.");
        }

        if (model.Status != ActivityStatus.Draft && model.Status != ActivityStatus.Rejected)
        {
            TempData["ErrorMessage"] = $"Hoạt động ở trạng thái '{model.Status}' không thể chỉnh sửa.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await LoadDropdownsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ActivityCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        var organizerId = GetCurrentUserId();
        var result = await _activityService.UpdateActivityAsync(model, organizerId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await LoadDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.ActivityId });
    }

    /// <summary>
    /// Màn hình 16 Stitch: Chi tiết quản trị hoạt động của Ban tổ chức
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _activityService.GetActivityDetailAsync(id, null);
        if (model == null)
        {
            return NotFound("Không tìm thấy hoạt động.");
        }

        return View(model);
    }

    /// <summary>
    /// Gửi hoạt động lên cấp Quản lý phê duyệt (DRAFT/REJECTED -> PENDING_APPROVAL)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        var organizerId = GetCurrentUserId();
        var result = await _activityService.SubmitForApprovalAsync(id, organizerId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Ban tổ chức chủ động bấm mở đăng ký sau khi được phê duyệt (APPROVED -> OPEN_REGISTRATION)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenRegistration(int id)
    {
        var organizerId = GetCurrentUserId();
        var result = await _activityService.OpenRegistrationAsync(id, organizerId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Màn hình 17 Stitch: Quản lý danh sách sinh viên đăng ký tham gia
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Registrations(int id)
    {
        var activity = await _context.Activities.FindAsync(id);
        if (activity == null) return NotFound("Không tìm thấy hoạt động.");

        ViewBag.ActivityId = activity.ActivityId;
        ViewBag.ActivityName = activity.Name;
        ViewBag.ActivityStatus = activity.Status;

        var list = await _registrationService.GetActivityRegistrationsAsync(id);
        return View(list);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.ActivityTypes = new SelectList(
            await _context.ActivityTypes.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(),
            "ActivityTypeId", "Name");

        ViewBag.Semesters = new SelectList(
            await _context.Semesters.OrderByDescending(s => s.StartDate).ToListAsync(),
            "SemesterId", "Name");
    }
}
