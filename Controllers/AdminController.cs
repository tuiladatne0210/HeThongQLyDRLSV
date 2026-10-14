using System.Security.Claims;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS_MVC.Controllers;

/// <summary>
/// Điều phối Cổng Quản trị viên (Quản lý tài khoản, kích hoạt, phân quyền role)
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
[Authorize(Roles = SystemRoles.Admin)]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Màn hình 29 Stitch: Quản lý người dùng và phân quyền tài khoản
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Users(string? statusFilter, string? roleFilter, string? keyword)
    {
        var model = await _adminService.GetUsersAsync(statusFilter, roleFilter, keyword);
        return View(model);
    }

    /// <summary>
    /// Giao diện xem và kích hoạt tài khoản / điều chỉnh quyền
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Activate(int id)
    {
        var model = await _adminService.GetUserForActivationAsync(id);
        if (model == null)
        {
            return NotFound("Không tìm thấy thông tin tài khoản.");
        }

        return View(model);
    }

    /// <summary>
    /// Kích hoạt tài khoản và cập nhật vai trò
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ActivateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var adminId = GetCurrentUserId();
        var result = await _adminService.UpdateUserStatusAndRolesAsync(model, adminId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Users));
        }

        ModelState.AddModelError(string.Empty, result.Message);
        return View(model);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
