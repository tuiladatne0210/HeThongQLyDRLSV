using EMS_MVC.Data;
using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Common;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Attendance;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ quản lý phiên điểm danh và ghi nhận điểm danh thủ công (BR04)
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Ngô Trọng Thiện, Lê Thành Đạt
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(ApplicationDbContext context, ILogger<AttendanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách các phiên điểm danh của hoạt động
    /// </summary>
    public async Task<ActivitySessionsViewModel?> GetActivitySessionsAsync(int activityId)
    {
        var activity = await _context.Activities
            .Include(a => a.AttendanceSessions)
                .ThenInclude(s => s.Attendances)
            .FirstOrDefaultAsync(a => a.ActivityId == activityId);

        if (activity == null) return null;

        return new ActivitySessionsViewModel
        {
            ActivityId = activity.ActivityId,
            ActivityName = activity.Name,
            ActivityStatus = activity.Status,
            AttendanceMode = activity.AttendanceMode,
            Sessions = activity.AttendanceSessions
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new AttendanceSessionItemViewModel
                {
                    SessionId = s.SessionId,
                    SessionName = s.SessionName,
                    StartsAt = s.StartsAt,
                    EndsAt = s.EndsAt,
                    Status = s.Status,
                    AttendeeCount = s.Attendances.Count(a => a.Status == AttendanceRecordStatus.Valid),
                    CreatedAt = s.CreatedAt,
                    ClosedAt = s.ClosedAt
                }).ToList(),
            NewSession = new SessionCreateViewModel
            {
                ActivityId = activityId,
                StartsAt = DateTime.UtcNow,
                EndsAt = DateTime.UtcNow.AddHours(2)
            }
        };
    }

    /// <summary>
    /// Tạo phiên điểm danh mới cho hoạt động
    /// </summary>
    public async Task<ServiceResult<int>> CreateSessionAsync(SessionCreateViewModel model, int organizerUserId)
    {
        try
        {
            var activity = await _context.Activities.FindAsync(model.ActivityId);
            if (activity == null)
            {
                return ServiceResult<int>.Failed("Hoạt động không tồn tại.");
            }

            if (model.StartsAt >= model.EndsAt)
            {
                return ServiceResult<int>.Failed("Thời gian bắt đầu phiên phải trước thời gian kết thúc.");
            }

            var session = new AttendanceSession
            {
                ActivityId = model.ActivityId,
                SessionName = model.SessionName.Trim(),
                StartsAt = model.StartsAt,
                EndsAt = model.EndsAt,
                Status = SessionStatus.Open,
                CreatedByUserId = organizerUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Organizer {UserId} đã tạo phiên điểm danh {SessionId} cho hoạt động {ActivityId}.",
                organizerUserId, session.SessionId, model.ActivityId);

            return ServiceResult<int>.Ok(session.SessionId, "Tạo phiên điểm danh thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo phiên điểm danh cho hoạt động {ActivityId}", model.ActivityId);
            return ServiceResult<int>.Failed("Đã xảy ra lỗi khi tạo phiên điểm danh.");
        }
    }

    /// <summary>
    /// Đóng phiên điểm danh
    /// </summary>
    public async Task<ServiceResult> CloseSessionAsync(int sessionId, int organizerUserId)
    {
        try
        {
            var session = await _context.AttendanceSessions.FindAsync(sessionId);
            if (session == null)
            {
                return ServiceResult.Failed("Phiên điểm danh không tồn tại.");
            }

            if (session.Status == SessionStatus.Closed)
            {
                return ServiceResult.Failed("Phiên điểm danh này đã được đóng trước đó.");
            }

            session.Status = SessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Phiên điểm danh {SessionId} đã được đóng bởi UserId {UserId}.", sessionId, organizerUserId);

            return ServiceResult.Ok("Đã đóng phiên điểm danh thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đóng phiên điểm danh {SessionId}", sessionId);
            return ServiceResult.Failed("Đã xảy ra lỗi khi đóng phiên điểm danh.");
        }
    }

    /// <summary>
    /// Lấy danh sách đăng ký để Ban tổ chức điểm danh thủ công
    /// </summary>
    public async Task<ManualAttendanceViewModel?> GetManualAttendanceViewModelAsync(int sessionId, string? keyword)
    {
        var session = await _context.AttendanceSessions
            .Include(s => s.Activity)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null) return null;

        var query = _context.ActivityRegistrations
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .Include(r => r.Student)
                .ThenInclude(s => s.Class)
                    .ThenInclude(c => c.Department)
            .Include(r => r.Attendances)
            .Where(r => r.ActivityId == session.ActivityId &&
                        (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended))
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(r => r.Student.StudentCode.ToLower().Contains(k) ||
                                     r.Student.User.FullName.ToLower().Contains(k) ||
                                     r.Student.Class.ClassCode.ToLower().Contains(k));
        }

        var registrations = await query
            .OrderBy(r => r.Student.StudentCode)
            .ToListAsync();

        return new ManualAttendanceViewModel
        {
            ActivityId = session.ActivityId,
            ActivityName = session.Activity.Name,
            SessionId = session.SessionId,
            SessionName = session.SessionName ?? "Phiên điểm danh",
            SessionStatus = session.Status,
            SearchKeyword = keyword,
            Registrations = registrations.Select(r =>
            {
                var att = r.Attendances.FirstOrDefault(a => a.SessionId == sessionId && a.Status == AttendanceRecordStatus.Valid);
                return new StudentRegistrationAttendanceItemViewModel
                {
                    RegistrationId = r.RegistrationId,
                    StudentId = r.StudentId,
                    StudentCode = r.Student.StudentCode,
                    FullName = r.Student.User.FullName,
                    ClassName = r.Student.Class.ClassName,
                    DepartmentName = r.Student.Class.Department.DepartmentName,
                    RegistrationStatus = r.Status,
                    IsCheckedIn = att != null,
                    CheckInAt = att?.CheckInAt,
                    Method = att?.Method
                };
            }).ToList()
        };
    }

    /// <summary>
    /// Ghi nhận điểm danh thủ công
    /// // BR04: Chỉ sinh viên có Registration hợp lệ cho Activity mới được ghi nhận điểm danh.
    /// // Chống điểm danh trùng trên cùng session (UQ_Attendances_Session_Registration).
    /// </summary>
    public async Task<ServiceResult> CheckInManualAsync(int sessionId, int registrationId, int recordedByUserId)
    {
        try
        {
            var session = await _context.AttendanceSessions.FindAsync(sessionId);
            if (session == null)
            {
                return ServiceResult.Failed("Phiên điểm danh không tồn tại.");
            }

            if (session.Status != SessionStatus.Open)
            {
                return ServiceResult.Failed("Phiên điểm danh này đã ĐÓNG, không thể ghi nhận thêm.");
            }

            // // BR04: Kiểm tra bản ghi đăng ký hợp lệ và thuộc đúng hoạt động của phiên điểm danh
            var reg = await _context.ActivityRegistrations
                .Include(r => r.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId && r.ActivityId == session.ActivityId);

            if (reg == null)
            {
                return ServiceResult.Failed("Bản ghi đăng ký không hợp lệ hoặc không thuộc hoạt động này.");
            }

            if (reg.Status == RegistrationStatus.Cancelled)
            {
                return ServiceResult.Failed("Sinh viên này đã hủy đăng ký, không thể điểm danh.");
            }

            // Kiểm tra trùng lặp điểm danh trên cùng phiên
            var alreadyCheckedIn = await _context.Attendances
                .AnyAsync(a => a.SessionId == sessionId && a.RegistrationId == registrationId && a.Status == AttendanceRecordStatus.Valid);

            if (alreadyCheckedIn)
            {
                return ServiceResult.Failed($"Sinh viên {reg.Student.StudentCode} đã được điểm danh trong phiên này trước đó.");
            }

            var now = DateTime.UtcNow;

            var attendance = new Attendance
            {
                SessionId = sessionId,
                RegistrationId = registrationId,
                ActivityId = session.ActivityId,
                CheckInAt = now,
                Method = AttendanceMethod.Manual,
                Status = AttendanceRecordStatus.Valid,
                RecordedByUserId = recordedByUserId,
                CreatedAt = now
            };

            _context.Attendances.Add(attendance);

            // Cập nhật trạng thái đăng ký của sinh viên thành ATTENDED
            reg.Status = RegistrationStatus.Attended;
            reg.UpdatedAt = now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("UserId {RecordedBy} đã điểm danh thủ công cho SV {StudentCode} (RegId: {RegId}) trong Session {SessionId}.",
                recordedByUserId, reg.Student.StudentCode, registrationId, sessionId);

            return ServiceResult.Ok($"Đã ghi nhận điểm danh thành công cho sinh viên {reg.Student.User.FullName} ({reg.Student.StudentCode}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi điểm danh thủ công Session {SessionId}, RegId {RegId}", sessionId, registrationId);
            return ServiceResult.Failed("Đã xảy ra lỗi khi ghi nhận điểm danh.");
        }
    }
}
