using EMS_MVC.Data;
using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Common;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ quản trị người dùng và phê duyệt kích hoạt tài khoản
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(ApplicationDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserManagementViewModel> GetUsersAsync(string? statusFilter, string? roleFilter, string? keyword)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.StudentProfile)
                .ThenInclude(s => s!.Class)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(u => u.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == roleFilter));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(k) ||
                                     u.FullName.ToLower().Contains(k) ||
                                     (u.Email != null && u.Email.ToLower().Contains(k)) ||
                                     (u.StudentProfile != null && u.StudentProfile.StudentCode.ToLower().Contains(k)));
        }

        var total = await _context.Users.CountAsync();
        var active = await _context.Users.CountAsync(u => u.Status == UserStatus.Active);
        var pending = await _context.Users.CountAsync(u => u.Status == UserStatus.PendingApproval);
        var disabled = await _context.Users.CountAsync(u => u.Status == UserStatus.Disabled || u.Status == UserStatus.Rejected);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListItemViewModel
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Status = u.Status,
                Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                StudentCode = u.StudentProfile != null ? u.StudentProfile.StudentCode : null,
                ClassName = u.StudentProfile != null ? u.StudentProfile.Class.ClassName : null,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        return new UserManagementViewModel
        {
            StatusFilter = statusFilter,
            RoleFilter = roleFilter,
            SearchKeyword = keyword,
            TotalUsers = total,
            ActiveUsers = active,
            PendingUsers = pending,
            DisabledUsers = disabled,
            Users = users
        };
    }

    public async Task<ActivateUserViewModel?> GetUserForActivationAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.StudentProfile)
                .ThenInclude(s => s!.Class)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return null;

        var allRoles = await _context.Roles.Select(r => r.RoleName).ToListAsync();

        return new ActivateUserViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            StudentCode = user.StudentProfile?.StudentCode,
            ClassName = user.StudentProfile?.Class.ClassName,
            CurrentStatus = user.Status,
            NewStatus = user.Status == UserStatus.PendingApproval ? UserStatus.Active : user.Status,
            SelectedRoles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
            AvailableRoles = allRoles
        };
    }

    public async Task<ServiceResult> UpdateUserStatusAndRolesAsync(ActivateUserViewModel model, int adminUserId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == model.UserId);

            if (user == null)
            {
                return ServiceResult.Failed("Không tìm thấy người dùng.");
            }

            var oldStatus = user.Status;
            user.Status = model.NewStatus;
            user.UpdatedAt = DateTime.UtcNow;

            // Cập nhật Roles
            var currentRoles = await _context.Roles.ToListAsync();
            var targetRoles = currentRoles.Where(r => model.SelectedRoles.Contains(r.RoleName)).ToList();

            // Xóa roles cũ không còn chọn
            var toRemove = user.UserRoles.Where(ur => !model.SelectedRoles.Contains(ur.Role.RoleName)).ToList();
            foreach (var ur in toRemove)
            {
                _context.UserRoles.Remove(ur);
            }

            // Thêm roles mới
            foreach (var r in targetRoles)
            {
                if (!user.UserRoles.Any(ur => ur.RoleId == r.RoleId))
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = r.RoleId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedByUserId = adminUserId
                    });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} đã cập nhật tài khoản {UserId} ({Username}) từ {OldStatus} sang {NewStatus}.",
                adminUserId, user.UserId, user.Username, oldStatus, user.Status);

            return ServiceResult.Ok($"Cập nhật tài khoản {user.Username} thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi Admin {AdminId} cập nhật tài khoản {UserId}", adminUserId, model.UserId);
            return ServiceResult.Failed("Đã xảy ra lỗi khi cập nhật tài khoản.");
        }
    }
}
