using System.Diagnostics;
using System.Security.Claims;
using EMS_MVC.Models;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EMS_MVC.Controllers;

public class HomeController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IAuthService _authService;

    public HomeController(IActivityService activityService, IAuthService authService)
    {
        _activityService = activityService;
        _authService = authService;
    }

    public async Task<IActionResult> Index()
    {
        int? studentId = null;
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(SystemRoles.Student))
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                studentId = await _authService.GetStudentIdByUserIdAsync(userId);
            }
        }

        var model = await _activityService.GetStudentActivitiesAsync(null, null, null, studentId);
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
