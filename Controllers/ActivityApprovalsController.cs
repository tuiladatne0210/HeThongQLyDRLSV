using System.Security.Claims;
using EMS_MVC.Data;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Controllers;

/// <summary>
/// Điều phối Cổng Cán bộ Quản lý (Phê duyệt và từ chối hoạt động ngoại khóa, Báo cáo quản lý)
/// Trách nhiệm chính: Lê Thành Hy (Nghiệp vụ); Ngô Trọng Thiện (Audit/Log, Báo cáo UC-028)
/// </summary>
[Authorize(Roles = $"{SystemRoles.Manager},{SystemRoles.Admin}")]
public class ActivityApprovalsController : Controller
{
    private readonly IActivityService _activityService;
    private readonly ApplicationDbContext _context;

    public ActivityApprovalsController(IActivityService activityService, ApplicationDbContext context)
    {
        _activityService = activityService;
        _context = context;
    }

    /// <summary>
    /// Màn hình Báo cáo quản lý tổng quan hoạt động và số liệu rèn luyện (UC-028)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Reports()
    {
        var totalActivities = await _context.Activities.CountAsync();
        var pendingCount = await _context.Activities.CountAsync(a => a.Status == ActivityStatus.PendingApproval);
        var approvedCount = await _context.Activities.CountAsync(a => a.Status == ActivityStatus.Approved);
        var rejectedCount = await _context.Activities.CountAsync(a => a.Status == ActivityStatus.Rejected);
        var openCount = await _context.Activities.CountAsync(a => a.Status == ActivityStatus.OpenRegistration);

        var totalRegistered = await _context.ActivityRegistrations.CountAsync(r => r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended);
        var totalAttended = await _context.ActivityRegistrations.CountAsync(r => r.Status == RegistrationStatus.Attended);

        var totalPoints = await _context.PointTransactions.SumAsync(pt => (long)pt.Points);
        if (totalPoints == 0)
        {
            totalPoints = await _context.ActivityRegistrations
                .Where(r => r.Status == RegistrationStatus.Attended)
                .SumAsync(r => (long)(r.Activity.PointOverride ?? 5));
        }

        var typeStats = await _context.ActivityTypes
            .Where(t => t.IsActive)
            .Select(t => new ActivityTypeStatViewModel
            {
                TypeName = t.Name,
                ActivityCount = t.Activities.Count,
                ParticipantCount = t.Activities.SelectMany(a => a.Registrations).Count(r => r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended)
            })
            .ToListAsync();

        var recentActivities = await _context.Activities
            .Include(a => a.ActivityType)
            .Include(a => a.Organizer)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new ActivityApprovalItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityCode = a.ActivityCode,
                Name = a.Name,
                ActivityTypeName = a.ActivityType.Name,
                OrganizerName = a.Organizer.FullName,
                Location = a.Location,
                StartAt = a.StartAt,
                EndAt = a.EndAt,
                MaxParticipants = a.MaxParticipants,
                Points = a.PointOverride ?? 5,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var model = new ManagerReportViewModel
        {
            TotalActivities = totalActivities,
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount,
            OpenRegistrationCount = openCount,
            TotalParticipantsRegistered = totalRegistered,
            TotalParticipantsAttended = totalAttended,
            TotalPointsAwarded = totalPoints,
            TypeStats = typeStats,
            RecentActivities = recentActivities
        };

        return View(model);
    }


    /// <summary>
    /// Màn hình 23 Stitch: Hàng đợi phê duyệt hoạt động
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await _activityService.GetApprovalQueueAsync();
        return View(model);
    }

    /// <summary>
    /// Xem chi tiết hoạt động trước khi ra quyết định phê duyệt
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var activity = await _activityService.GetActivityDetailAsync(id, null);
        if (activity == null)
        {
            return NotFound("Không tìm thấy hoạt động cần xem xét.");
        }

        ViewBag.DecisionModel = new ActivityReviewDecisionViewModel { ActivityId = id };
        return View(activity);
    }

    /// <summary>
    /// Xử lý quyết định phê duyệt hoặc từ chối (Từ chối bắt buộc có lý do)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decision(ActivityReviewDecisionViewModel model)
    {
        var managerId = GetCurrentUserId();

        if (model.Action.ToUpperInvariant() == "APPROVE")
        {
            var result = await _activityService.ApproveActivityAsync(model.ActivityId, managerId, model.ReviewNote);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = result.Message;
        }
        else if (model.Action.ToUpperInvariant() == "REJECT")
        {
            if (string.IsNullOrWhiteSpace(model.ReviewNote))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối để gửi lại cho Ban tổ chức điều chỉnh.";
                return RedirectToAction(nameof(Review), new { id = model.ActivityId });
            }

            var result = await _activityService.RejectActivityAsync(model.ActivityId, managerId, model.ReviewNote);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Review), new { id = model.ActivityId });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
