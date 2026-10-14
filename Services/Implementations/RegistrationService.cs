using EMS_MVC.Data;
using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Common;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Activity;
using EMS_MVC.ViewModels.Attendance;
using EMS_MVC.ViewModels.Student;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EMS_MVC.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ đăng ký và hủy đăng ký với đầy đủ Business Rules BR01, BR02, BR03
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Lê Thành Đạt
/// </summary>
public class RegistrationService : IRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(ApplicationDbContext context, ILogger<RegistrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký tham gia hoạt động
    /// // BR01: Mỗi Student chỉ có 1 Registration cho 1 Activity.
    /// // BR02: Không nhận đăng ký ngoài thời hạn cho phép.
    /// // BR03: Không vượt quá số lượng người tham gia tối đa (kiểm tra an toàn trong Transaction).
    /// </summary>
    public async Task<ServiceResult> RegisterAsync(int activityId, int studentId)
    {
        IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
        {
            transaction = await _context.Database.BeginTransactionAsync();
        }

        try
        {
            var activity = await _context.Activities
                .Include(a => a.Registrations)
                .FirstOrDefaultAsync(a => a.ActivityId == activityId);

            if (activity == null)
            {
                return ServiceResult.Failed("Hoạt động không tồn tại trong hệ thống.");
            }

            // Kiểm tra trạng thái hoạt động
            if (activity.Status != ActivityStatus.OpenRegistration)
            {
                return ServiceResult.Failed($"Hoạt động hiện không ở trạng thái mở nhận đăng ký (Trạng thái: {activity.Status}).");
            }

            var now = DateTime.UtcNow;

            // // BR02: Kiểm tra khung thời gian đăng ký
            if (now < activity.RegistrationStartAt)
            {
                return ServiceResult.Failed($"Chưa đến thời gian mở nhận đăng ký (Bắt đầu từ {activity.RegistrationStartAt:dd/MM/yyyy HH:mm}).");
            }

            if (now > activity.RegistrationEndAt)
            {
                return ServiceResult.Failed($"Đã hết thời gian nhận đăng ký (Hạn chót là {activity.RegistrationEndAt:dd/MM/yyyy HH:mm}).");
            }

            // // BR01: Kiểm tra đăng ký trùng lặp của sinh viên trên cùng hoạt động
            var existingReg = await _context.ActivityRegistrations
                .FirstOrDefaultAsync(r => r.ActivityId == activityId && r.StudentId == studentId);

            if (existingReg != null)
            {
                if (existingReg.Status == RegistrationStatus.Registered || existingReg.Status == RegistrationStatus.Attended)
                {
                    return ServiceResult.Failed("Bạn đã đăng ký tham gia hoạt động này rồi (Không được đăng ký trùng).");
                }
            }

            // // BR03: Kiểm tra sức chứa tối đa trong cùng Transaction để chống tranh chấp đồng thời
            var currentActiveCount = await _context.ActivityRegistrations
                .CountAsync(r => r.ActivityId == activityId &&
                                (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended));

            if (activity.MaxParticipants.HasValue && currentActiveCount >= activity.MaxParticipants.Value)
            {
                return ServiceResult.Failed($"Rất tiếc! Hoạt động đã đủ số lượng đăng ký tối đa ({activity.MaxParticipants.Value} người).");
            }

            if (existingReg != null && existingReg.Status == RegistrationStatus.Cancelled)
            {
                // Tái kích hoạt bản ghi hiện có để thỏa mãn unique constraint (ActivityId, StudentId) trong database
                existingReg.Status = RegistrationStatus.Registered;
                existingReg.CancelledAt = null;
                existingReg.RegisteredAt = now;
                existingReg.UpdatedAt = now;
            }
            else
            {
                // Tạo mới bản ghi đăng ký
                var newRegistration = new ActivityRegistration
                {
                    ActivityId = activityId,
                    StudentId = studentId,
                    Status = RegistrationStatus.Registered,
                    RegisteredAt = now
                };
                _context.ActivityRegistrations.Add(newRegistration);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            _logger.LogInformation("Sinh viên {StudentId} đã đăng ký thành công hoạt động {ActivityId}.", studentId, activityId);
            return ServiceResult.Ok("Đăng ký tham gia hoạt động thành công!");
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            _logger.LogError(ex, "Lỗi khi sinh viên {StudentId} đăng ký hoạt động {ActivityId}", studentId, activityId);
            return ServiceResult.Failed("Đã xảy ra lỗi trong quá trình xử lý đăng ký. Vui lòng thử lại.");
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Hủy đăng ký tham gia hoạt động trước hạn chót cho phép
    /// </summary>
    public async Task<ServiceResult> CancelRegistrationAsync(int activityId, int studentId)
    {
        try
        {
            var activity = await _context.Activities.FindAsync(activityId);
            if (activity == null)
            {
                return ServiceResult.Failed("Hoạt động không tồn tại.");
            }

            var reg = await _context.ActivityRegistrations
                .FirstOrDefaultAsync(r => r.ActivityId == activityId && r.StudentId == studentId);

            if (reg == null || reg.Status != RegistrationStatus.Registered)
            {
                return ServiceResult.Failed("Không tìm thấy đăng ký hợp lệ cần hủy.");
            }

            var now = DateTime.UtcNow;

            // Kiểm tra thời hạn cho phép hủy đăng ký
            if (now > activity.RegistrationEndAt)
            {
                return ServiceResult.Failed("Đã quá thời hạn cho phép hủy đăng ký hoạt động.");
            }

            reg.Status = RegistrationStatus.Cancelled;
            reg.CancelledAt = now;
            reg.UpdatedAt = now;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sinh viên {StudentId} đã hủy đăng ký hoạt động {ActivityId}.", studentId, activityId);

            return ServiceResult.Ok("Hủy đăng ký tham gia hoạt động thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hủy đăng ký hoạt động {ActivityId} của sinh viên {StudentId}", activityId, studentId);
            return ServiceResult.Failed("Đã xảy ra lỗi khi hủy đăng ký.");
        }
    }

    /// <summary>
    /// Lấy danh sách các hoạt động sinh viên đã đăng ký
    /// </summary>
    public async Task<List<StudentActivityItemViewModel>> GetStudentRegistrationsAsync(int studentId)
    {
        return await _context.ActivityRegistrations
            .Include(r => r.Activity)
                .ThenInclude(a => a.ActivityType)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.RegisteredAt)
            .Select(r => new StudentActivityItemViewModel
            {
                ActivityId = r.Activity.ActivityId,
                ActivityCode = r.Activity.ActivityCode,
                Name = r.Activity.Name,
                ActivityTypeName = r.Activity.ActivityType.Name,
                Location = r.Activity.Location,
                StartAt = r.Activity.StartAt,
                EndAt = r.Activity.EndAt,
                RegistrationEndAt = r.Activity.RegistrationEndAt,
                Points = r.Activity.PointOverride ?? 5,
                Status = r.Activity.Status,
                RegistrationStatus = r.Status
            })
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh sách sinh viên đăng ký của hoạt động phục vụ Ban tổ chức
    /// </summary>
    public async Task<List<StudentRegistrationAttendanceItemViewModel>> GetActivityRegistrationsAsync(int activityId)
    {
        return await _context.ActivityRegistrations
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .Include(r => r.Student)
                .ThenInclude(s => s.Class)
                    .ThenInclude(c => c.Department)
            .Where(r => r.ActivityId == activityId)
            .OrderBy(r => r.RegisteredAt)
            .Select(r => new StudentRegistrationAttendanceItemViewModel
            {
                RegistrationId = r.RegistrationId,
                StudentId = r.StudentId,
                StudentCode = r.Student.StudentCode,
                FullName = r.Student.User.FullName,
                ClassName = r.Student.Class.ClassName,
                DepartmentName = r.Student.Class.Department.DepartmentName,
                RegistrationStatus = r.Status
            })
            .ToListAsync();
    }
}
