using System.Security.Claims;
using EMS_MVC.Data;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Controllers;

/// <summary>
/// Điều phối luồng Xác thực, Đăng nhập, Đăng ký và Quản lý phiên Cookie
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Lê Thành Đạt
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService _authService,
        ApplicationDbContext context,
        ILogger<AccountController> logger)
    {
        this._authService = _authService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultRolePage();
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var authResult = await _authService.AuthenticateUserAsync(model);
        if (!authResult.Success || authResult.Data == null)
        {
            ModelState.AddModelError(string.Empty, authResult.Message);
            return View(model);
        }

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        // Ghi nhận phiên đăng nhập bằng Cookie Authentication
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authResult.Data,
            authProperties);

        _logger.LogInformation("Người dùng {Username} đã đăng nhập thành công qua Cookie.", model.Username);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToDefaultRolePage(authResult.Data);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultRolePage();
        }

        await LoadClassesDropdownAsync();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadClassesDropdownAsync();
            return View(model);
        }

        var regResult = await _authService.RegisterStudentAsync(model);
        if (!regResult.Success)
        {
            ModelState.AddModelError(string.Empty, regResult.Message);
            await LoadClassesDropdownAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = regResult.Message;
        return RedirectToAction(nameof(PendingApproval));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult PendingApproval()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Người dùng {User} đã đăng xuất.", User.Identity?.Name);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _authService.ChangePasswordAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
        return RedirectToAction(nameof(ChangePassword));
    }

    private IActionResult RedirectToDefaultRolePage(ClaimsPrincipal? principal = null)
    {
        var user = principal ?? User;
        if (user.IsInRole(SystemRoles.Admin))
        {
            return RedirectToAction("Users", "Admin");
        }
        if (user.IsInRole(SystemRoles.Manager))
        {
            return RedirectToAction("Index", "ActivityApprovals");
        }
        if (user.IsInRole(SystemRoles.Organizer))
        {
            return RedirectToAction("Index", "OrganizerActivities");
        }
        return RedirectToAction("Dashboard", "Student");
    }

    private async Task LoadClassesDropdownAsync()
    {
        var classes = await _context.Classes
            .Include(c => c.Department)
            .Where(c => c.IsActive)
            .OrderBy(c => c.ClassCode)
            .Select(c => new
            {
                c.ClassId,
                DisplayName = $"{c.ClassCode} - {c.ClassName} ({c.Department.DepartmentCode})"
            })
            .ToListAsync();

        ViewBag.ClassesList = new SelectList(classes, "ClassId", "DisplayName");
    }
}
