using System.Security.Claims;
using EMS_MVC.Data;
using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Common;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Account;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ xác thực tài khoản và phiên đăng nhập Cookie
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Ngô Trọng Thiện
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký tài khoản sinh viên mới, lưu mật khẩu đã băm an toàn và đặt trạng thái PENDING_APPROVAL
    /// </summary>
    public async Task<ServiceResult> RegisterStudentAsync(RegisterViewModel model)
    {
        try
        {
            var normalizedUsername = model.Username.Trim().ToUpperInvariant();
            var normalizedEmail = model.Email.Trim().ToUpperInvariant();
            var studentCode = model.StudentCode.Trim().ToUpperInvariant();

            // Kiểm tra trùng username
            if (await _context.Users.AnyAsync(u => u.NormalizedUsername == normalizedUsername))
            {
                return ServiceResult.Failed($"Tên đăng nhập '{model.Username}' đã tồn tại trong hệ thống.");
            }

            // Kiểm tra trùng mã sinh viên
            if (await _context.Students.AnyAsync(s => s.StudentCode == studentCode))
            {
                return ServiceResult.Failed($"Mã sinh viên '{model.StudentCode}' đã được đăng ký tài khoản.");
            }

            // Kiểm tra trùng email
            if (await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
            {
                return ServiceResult.Failed($"Email '{model.Email}' đã được sử dụng.");
            }

            // Kiểm tra lớp sinh hoạt
            var classExists = await _context.Classes.AnyAsync(c => c.ClassId == model.ClassId && c.IsActive);
            if (!classExists)
            {
                return ServiceResult.Failed("Lớp học được chọn không hợp lệ hoặc đã ngưng hoạt động.");
            }

            var roleStudent = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SystemRoles.Student);
            if (roleStudent == null)
            {
                return ServiceResult.Failed("Không tìm thấy vai trò Sinh viên trong hệ thống.");
            }

            // Tạo người dùng mới với trạng thái PENDING_APPROVAL
            var user = new User
            {
                Username = model.Username.Trim(),
                NormalizedUsername = normalizedUsername,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PhoneNumber = model.PhoneNumber?.Trim(),
                Status = UserStatus.PendingApproval,
                EmailConfirmed = false,
                AccessFailedCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            // Băm mật khẩu bằng ASP.NET Core PasswordHasher (PBKDF2/HMAC-SHA256, không lưu plain-text)
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Gán role mặc định là Student
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = roleStudent.RoleId,
                AssignedAt = DateTime.UtcNow
            });

            // Tạo hồ sơ sinh viên
            var student = new Student
            {
                UserId = user.UserId,
                StudentCode = studentCode,
                DateOfBirth = model.DateOfBirth,
                ClassId = model.ClassId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Students.Add(student);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Sinh viên mới {Username} (MSSV: {StudentCode}) đã đăng ký thành công, trạng thái PENDING_APPROVAL.",
                user.Username, student.StudentCode);

            return ServiceResult.Ok("Đăng ký tài khoản thành công! Tài khoản đang chờ Quản trị viên kiểm tra và kích hoạt trước khi có thể đăng nhập.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng ký tài khoản sinh viên {Username}", model.Username);
            return ServiceResult.Failed("Đã xảy ra lỗi hệ thống trong quá trình đăng ký. Vui lòng thử lại sau.");
        }
    }

    /// <summary>
    /// Xác thực thông tin đăng nhập, kiểm tra mật khẩu và tạo ClaimsPrincipal cho Cookie
    /// </summary>
    public async Task<ServiceResult<ClaimsPrincipal>> AuthenticateUserAsync(LoginViewModel model)
    {
        try
        {
            var loginInput = model.Username.Trim();
            var normalizedInput = loginInput.ToUpperInvariant();

            // Tìm theo Username hoặc Mã sinh viên
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedInput ||
                                          (u.StudentProfile != null && u.StudentProfile.StudentCode == normalizedInput) ||
                                          u.NormalizedEmail == normalizedInput);

            if (user == null)
            {
                return ServiceResult<ClaimsPrincipal>.Failed("Tên đăng nhập, mã sinh viên hoặc mật khẩu không chính xác.");
            }

            // Kiểm tra mật khẩu đã băm
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                user.AccessFailedCount++;
                await _context.SaveChangesAsync();
                return ServiceResult<ClaimsPrincipal>.Failed("Tên đăng nhập, mã sinh viên hoặc mật khẩu không chính xác.");
            }

            // Kiểm tra trạng thái tài khoản
            if (user.Status == UserStatus.PendingApproval)
            {
                return ServiceResult<ClaimsPrincipal>.Failed("Tài khoản của bạn đang ở trạng thái CHỜ DUYỆT. Vui lòng liên hệ Quản trị viên để được kích hoạt trước khi sử dụng.");
            }

            if (user.Status == UserStatus.Rejected)
            {
                return ServiceResult<ClaimsPrincipal>.Failed("Tài khoản của bạn đã bị TỪ CHỐI kích hoạt. Vui lòng liên hệ Văn phòng Đào tạo / CTSV.");
            }

            if (user.Status == UserStatus.Disabled)
            {
                return ServiceResult<ClaimsPrincipal>.Failed("Tài khoản của bạn hiện đang bị TẠM KHÓA.");
            }

            if (user.Status != UserStatus.Active)
            {
                return ServiceResult<ClaimsPrincipal>.Failed("Trạng thái tài khoản không hợp lệ để đăng nhập.");
            }

            // Cập nhật thông tin đăng nhập thành công
            user.AccessFailedCount = 0;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Xây dựng danh sách Claims cho Cookie Authentication
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new("Username", user.Username),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            if (user.StudentProfile != null)
            {
                claims.Add(new Claim("StudentId", user.StudentProfile.StudentId.ToString()));
                claims.Add(new Claim("StudentCode", user.StudentProfile.StudentCode));
            }

            // Hỗ trợ người dùng có nhiều vai trò (Multi-role)
            foreach (var ur in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, ur.Role.RoleName));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            return ServiceResult<ClaimsPrincipal>.Ok(principal, "Đăng nhập thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi đăng nhập cho tài khoản {Input}", model.Username);
            return ServiceResult<ClaimsPrincipal>.Failed("Đã xảy ra lỗi máy chủ trong quá trình xác thực đăng nhập.");
        }
    }

    /// <summary>
    /// Đổi mật khẩu tài khoản
    /// </summary>
    public async Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordViewModel model)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failed("Không tìm thấy thông tin tài khoản.");
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return ServiceResult.Failed("Mật khẩu hiện tại không chính xác.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đổi mật khẩu thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đổi mật khẩu cho UserId {UserId}", userId);
            return ServiceResult.Failed("Đã xảy ra lỗi khi đổi mật khẩu.");
        }
    }

    public async Task<List<string>> GetUserRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.RoleName)
            .ToListAsync();
    }

    public async Task<int?> GetStudentIdByUserIdAsync(int userId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        return student?.StudentId;
    }
}
