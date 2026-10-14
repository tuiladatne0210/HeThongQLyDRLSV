using EMS_MVC.Data;
using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Common;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Activity;
using EMS_MVC.ViewModels.Manager;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ quản lý hoạt động và quy trình phê duyệt
/// Trách nhiệm chính: Lê Thành Hy; Review & phối hợp: Lê Thành Đạt
/// </summary>
public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(ApplicationDbContext context, ILogger<ActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách hoạt động công khai hiển thị cho sinh viên
    /// </summary>
    public async Task<ActivityListViewModel> GetStudentActivitiesAsync(string? search, int? typeId, int? semesterId, int? studentId)
    {
        var query = _context.Activities
            .Include(a => a.ActivityType)
            .Include(a => a.Organizer)
            .Include(a => a.Registrations)
            .Where(a => a.Status == ActivityStatus.OpenRegistration ||
                        a.Status == ActivityStatus.Ongoing ||
                        a.Status == ActivityStatus.RegistrationClosed ||
                        a.Status == ActivityStatus.Completed)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(keyword) ||
                                     (a.ActivityCode != null && a.ActivityCode.ToLower().Contains(keyword)) ||
                                     a.Location.ToLower().Contains(keyword));
        }

        if (typeId.HasValue && typeId > 0)
        {
            query = query.Where(a => a.ActivityTypeId == typeId.Value);
        }

        if (semesterId.HasValue && semesterId > 0)
        {
            query = query.Where(a => a.SemesterId == semesterId.Value);
        }

        var activities = await query
            .OrderByDescending(a => a.StartAt)
            .ToListAsync();

        var result = new ActivityListViewModel
        {
            SearchKeyword = search,
            ActivityTypeId = typeId,
            SemesterId = semesterId,
            Activities = activities.Select(a =>
            {
                var reg = studentId.HasValue
                    ? a.Registrations.FirstOrDefault(r => r.StudentId == studentId.Value)
                    : null;

                return new ActivityCardItemViewModel
                {
                    ActivityId = a.ActivityId,
                    ActivityCode = a.ActivityCode,
                    Name = a.Name,
                    ActivityTypeName = a.ActivityType.Name,
                    OrganizerName = a.Organizer.FullName,
                    Location = a.Location,
                    StartAt = a.StartAt,
                    EndAt = a.EndAt,
                    RegistrationStartAt = a.RegistrationStartAt,
                    RegistrationEndAt = a.RegistrationEndAt,
                    MaxParticipants = a.MaxParticipants,
                    CurrentParticipantsCount = a.Registrations.Count(r => r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended),
                    Points = a.PointOverride ?? 5,
                    Status = a.Status,
                    StudentRegistrationStatus = reg?.Status
                };
            }).ToList()
        };

        return result;
    }

    /// <summary>
    /// Xem chi tiết hoạt động và kiểm tra điều kiện đăng ký của sinh viên
    /// </summary>
    public async Task<ActivityDetailViewModel?> GetActivityDetailAsync(int activityId, int? studentId)
    {
        var activity = await _context.Activities
            .Include(a => a.ActivityType)
            .Include(a => a.Organizer)
            .Include(a => a.Semester)
            .Include(a => a.Registrations)
            .FirstOrDefaultAsync(a => a.ActivityId == activityId);

        if (activity == null) return null;

        var now = DateTime.UtcNow;
        var validRegistrationsCount = activity.Registrations.Count(r => r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended);

        var studentReg = studentId.HasValue
            ? activity.Registrations.FirstOrDefault(r => r.StudentId == studentId.Value)
            : null;

        var isRegistered = studentReg != null && (studentReg.Status == RegistrationStatus.Registered || studentReg.Status == RegistrationStatus.Attended);

        // Kiểm tra điều kiện đăng ký
        bool canRegister = false;
        bool canCancel = false;
        string? cannotReason = null;

        if (studentId.HasValue)
        {
            if (isRegistered)
            {
                cannotReason = "Bạn đã đăng ký tham gia hoạt động này.";
                // Cho phép hủy nếu trước hạn chót đăng ký
                if (now <= activity.RegistrationEndAt && studentReg?.Status == RegistrationStatus.Registered)
                {
                    canCancel = true;
                }
            }
            else
            {
                // Kiểm tra trạng thái hoạt động
                if (activity.Status != ActivityStatus.OpenRegistration)
                {
                    cannotReason = "Hoạt động hiện không ở trạng thái mở nhận đăng ký.";
                }
                // // BR02: Kiểm tra khung thời gian đăng ký
                else if (now < activity.RegistrationStartAt)
                {
                    cannotReason = $"Chưa đến thời gian mở đăng ký (Mở lúc: {activity.RegistrationStartAt:dd/MM/yyyy HH:mm}).";
                }
                else if (now > activity.RegistrationEndAt)
                {
                    cannotReason = "Thời gian đăng ký tham gia đã kết thúc.";
                }
                // // BR03: Kiểm tra giới hạn số lượng đăng ký tối đa
                else if (activity.MaxParticipants.HasValue && validRegistrationsCount >= activity.MaxParticipants.Value)
                {
                    cannotReason = "Hoạt động đã đạt số lượng đăng ký tối đa.";
                }
                else
                {
                    canRegister = true;
                }
            }
        }

        return new ActivityDetailViewModel
        {
            ActivityId = activity.ActivityId,
            ActivityCode = activity.ActivityCode,
            Name = activity.Name,
            Description = activity.Description,
            ActivityTypeName = activity.ActivityType.Name,
            OrganizerName = activity.Organizer.FullName,
            SemesterName = activity.Semester.Name,
            Location = activity.Location,
            StartAt = activity.StartAt,
            EndAt = activity.EndAt,
            RegistrationStartAt = activity.RegistrationStartAt,
            RegistrationEndAt = activity.RegistrationEndAt,
            MaxParticipants = activity.MaxParticipants,
            CurrentParticipantsCount = validRegistrationsCount,
            Points = activity.PointOverride ?? 5,
            AttendanceMode = activity.AttendanceMode,
            Status = activity.Status,
            ReviewNote = activity.ReviewNote,
            IsStudent = studentId.HasValue,
            IsRegistered = isRegistered,
            RegistrationStatus = studentReg?.Status,
            RegisteredAt = studentReg?.RegisteredAt,
            CanRegister = canRegister,
            CanCancel = canCancel,
            CannotRegisterReason = cannotReason
        };
    }

    /// <summary>
    /// Lấy danh sách hoạt động do Ban tổ chức hiện tại phụ trách
    /// </summary>
    public async Task<List<ActivityCardItemViewModel>> GetOrganizerActivitiesAsync(int organizerUserId)
    {
        return await _context.Activities
            .Include(a => a.ActivityType)
            .Include(a => a.Organizer)
            .Include(a => a.Registrations)
            .Where(a => a.OrganizerId == organizerUserId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ActivityCardItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityCode = a.ActivityCode,
                Name = a.Name,
                ActivityTypeName = a.ActivityType.Name,
                OrganizerName = a.Organizer.FullName,
                Location = a.Location,
                StartAt = a.StartAt,
                EndAt = a.EndAt,
                RegistrationStartAt = a.RegistrationStartAt,
                RegistrationEndAt = a.RegistrationEndAt,
                MaxParticipants = a.MaxParticipants,
                CurrentParticipantsCount = a.Registrations.Count(r => r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended),
                Points = a.PointOverride ?? 5,
                Status = a.Status
            })
            .ToListAsync();
    }

    public async Task<ActivityCreateEditViewModel?> GetActivityForEditAsync(int activityId, int organizerUserId)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activityId && a.OrganizerId == organizerUserId);
        if (activity == null) return null;

        return new ActivityCreateEditViewModel
        {
            ActivityId = activity.ActivityId,
            ActivityCode = activity.ActivityCode,
            Name = activity.Name,
            Description = activity.Description,
            ActivityTypeId = activity.ActivityTypeId,
            SemesterId = activity.SemesterId,
            Location = activity.Location,
            StartAt = activity.StartAt,
            EndAt = activity.EndAt,
            RegistrationStartAt = activity.RegistrationStartAt,
            RegistrationEndAt = activity.RegistrationEndAt,
            MaxParticipants = activity.MaxParticipants,
            PointOverride = activity.PointOverride,
            AttendanceMode = activity.AttendanceMode,
            Status = activity.Status,
            ReviewNote = activity.ReviewNote
        };
    }

    /// <summary>
    /// Tạo hoạt động mới ở trạng thái DRAFT
    /// </summary>
    public async Task<ServiceResult<int>> CreateActivityAsync(ActivityCreateEditViewModel model, int organizerUserId)
    {
        try
        {
            // Kiểm tra tính hợp lệ về logic thời gian
            if (model.StartAt >= model.EndAt)
            {
                return ServiceResult<int>.Failed("Thời gian bắt đầu hoạt động phải trước thời gian kết thúc.");
            }

            if (model.RegistrationStartAt > model.RegistrationEndAt)
            {
                return ServiceResult<int>.Failed("Thời gian mở đăng ký phải trước hạn chót đăng ký.");
            }

            if (model.RegistrationEndAt > model.StartAt)
            {
                return ServiceResult<int>.Failed("Hạn chót đăng ký không được vượt quá thời gian bắt đầu hoạt động.");
            }

            var activity = new Activity
            {
                ActivityCode = string.IsNullOrWhiteSpace(model.ActivityCode) ? null : model.ActivityCode.Trim(),
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                ActivityTypeId = model.ActivityTypeId,
                OrganizerId = organizerUserId,
                SemesterId = model.SemesterId,
                Location = model.Location.Trim(),
                StartAt = model.StartAt,
                EndAt = model.EndAt,
                RegistrationStartAt = model.RegistrationStartAt,
                RegistrationEndAt = model.RegistrationEndAt,
                MaxParticipants = model.MaxParticipants,
                PointOverride = model.PointOverride,
                AttendanceMode = model.AttendanceMode,
                Status = ActivityStatus.Draft, // Khởi tạo ban đầu luôn là DRAFT
                CreatedAt = DateTime.UtcNow
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Organizer {UserId} đã tạo hoạt động {ActivityId}: {Name} ở trạng thái DRAFT.",
                organizerUserId, activity.ActivityId, activity.Name);

            return ServiceResult<int>.Ok(activity.ActivityId, "Tạo hoạt động mới thành công (Lưu bản nháp DRAFT).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo hoạt động mới");
            return ServiceResult<int>.Failed("Đã xảy ra lỗi khi tạo hoạt động.");
        }
    }

    /// <summary>
    /// Chỉnh sửa hoạt động (chỉ cho phép khi ở trạng thái DRAFT hoặc REJECTED)
    /// </summary>
    public async Task<ServiceResult> UpdateActivityAsync(ActivityCreateEditViewModel model, int organizerUserId)
    {
        try
        {
            if (!model.ActivityId.HasValue)
            {
                return ServiceResult.Failed("Không tìm thấy mã hoạt động cần sửa.");
            }

            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == model.ActivityId.Value && a.OrganizerId == organizerUserId);
            if (activity == null)
            {
                return ServiceResult.Failed("Hoạt động không tồn tại hoặc bạn không có quyền chỉnh sửa.");
            }

            // Quy tắc: Chỉ cho phép chỉnh sửa khi DRAFT hoặc REJECTED
            if (activity.Status != ActivityStatus.Draft && activity.Status != ActivityStatus.Rejected)
            {
                return ServiceResult.Failed($"Không thể chỉnh sửa hoạt động đang ở trạng thái '{activity.Status}'. Chỉ được sửa bản nháp hoặc hoạt động bị từ chối.");
            }

            if (model.StartAt >= model.EndAt)
            {
                return ServiceResult.Failed("Thời gian bắt đầu hoạt động phải trước thời gian kết thúc.");
            }

            if (model.RegistrationStartAt > model.RegistrationEndAt || model.RegistrationEndAt > model.StartAt)
            {
                return ServiceResult.Failed("Khung thời gian nhận đăng ký không hợp lệ.");
            }

            activity.ActivityCode = string.IsNullOrWhiteSpace(model.ActivityCode) ? null : model.ActivityCode.Trim();
            activity.Name = model.Name.Trim();
            activity.Description = model.Description?.Trim();
            activity.ActivityTypeId = model.ActivityTypeId;
            activity.SemesterId = model.SemesterId;
            activity.Location = model.Location.Trim();
            activity.StartAt = model.StartAt;
            activity.EndAt = model.EndAt;
            activity.RegistrationStartAt = model.RegistrationStartAt;
            activity.RegistrationEndAt = model.RegistrationEndAt;
            activity.MaxParticipants = model.MaxParticipants;
            activity.PointOverride = model.PointOverride;
            activity.AttendanceMode = model.AttendanceMode;
            activity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cập nhật thông tin hoạt động thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật hoạt động {ActivityId}", model.ActivityId);
            return ServiceResult.Failed("Đã xảy ra lỗi khi cập nhật hoạt động.");
        }
    }

    /// <summary>
    /// Gửi hoạt động để Manager phê duyệt (DRAFT/REJECTED -> PENDING_APPROVAL)
    /// </summary>
    public async Task<ServiceResult> SubmitForApprovalAsync(int activityId, int organizerUserId)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activityId && a.OrganizerId == organizerUserId);
        if (activity == null)
        {
            return ServiceResult.Failed("Không tìm thấy hoạt động hoặc bạn không có quyền thao tác.");
        }

        if (activity.Status != ActivityStatus.Draft && activity.Status != ActivityStatus.Rejected)
        {
            return ServiceResult.Failed($"Chỉ có hoạt động Bản nháp (DRAFT) hoặc Bị từ chối (REJECTED) mới có thể gửi duyệt. Trạng thái hiện tại: {activity.Status}");
        }

        activity.Status = ActivityStatus.PendingApproval;
        activity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Hoạt động {ActivityId} đã được gửi duyệt bởi Organizer {UserId}.", activityId, organizerUserId);

        return ServiceResult.Ok("Đã gửi hoạt động lên Ban Quản lý để phê duyệt.");
    }

    /// <summary>
    /// Ban tổ chức chủ động bấm mở đăng ký sau khi hoạt động được duyệt (APPROVED -> OPEN_REGISTRATION)
    /// </summary>
    public async Task<ServiceResult> OpenRegistrationAsync(int activityId, int organizerUserId)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activityId && a.OrganizerId == organizerUserId);
        if (activity == null)
        {
            return ServiceResult.Failed("Không tìm thấy hoạt động hoặc bạn không có quyền.");
        }

        if (activity.Status != ActivityStatus.Approved)
        {
            return ServiceResult.Failed($"Chỉ hoạt động đã được phê duyệt (APPROVED) mới có thể mở đăng ký. Trạng thái hiện tại: {activity.Status}");
        }

        activity.Status = ActivityStatus.OpenRegistration;
        activity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Hoạt động {ActivityId} đã được mở đăng ký bởi Organizer {UserId}.", activityId, organizerUserId);

        return ServiceResult.Ok("Đã mở đăng ký hoạt động cho sinh viên tham gia!");
    }

    /// <summary>
    /// Lấy danh sách hàng đợi phê duyệt hoạt động cho Manager
    /// </summary>
    public async Task<ActivityApprovalQueueViewModel> GetApprovalQueueAsync()
    {
        var pending = await _context.Activities
            .Include(a => a.ActivityType)
            .Include(a => a.Organizer)
            .Include(a => a.Semester)
            .Where(a => a.Status == ActivityStatus.PendingApproval)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new ActivityApprovalItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityCode = a.ActivityCode,
                Name = a.Name,
                ActivityTypeName = a.ActivityType.Name,
                OrganizerName = a.Organizer.FullName,
                SemesterName = a.Semester.Name,
                Location = a.Location,
                StartAt = a.StartAt,
                EndAt = a.EndAt,
                MaxParticipants = a.MaxParticipants,
                Points = a.PointOverride ?? 5,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var recent = await _context.Activities
            .Include(a => a.ActivityType)
            .Include(a => a.Organizer)
            .Include(a => a.Semester)
            .Where(a => a.Status == ActivityStatus.Approved || a.Status == ActivityStatus.Rejected)
            .OrderByDescending(a => a.ReviewedAt)
            .Take(10)
            .Select(a => new ActivityApprovalItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityCode = a.ActivityCode,
                Name = a.Name,
                ActivityTypeName = a.ActivityType.Name,
                OrganizerName = a.Organizer.FullName,
                SemesterName = a.Semester.Name,
                Location = a.Location,
                StartAt = a.StartAt,
                EndAt = a.EndAt,
                MaxParticipants = a.MaxParticipants,
                Points = a.PointOverride ?? 5,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                ReviewNote = a.ReviewNote,
                ReviewedAt = a.ReviewedAt
            })
            .ToListAsync();

        return new ActivityApprovalQueueViewModel
        {
            PendingActivities = pending,
            RecentReviewedActivities = recent
        };
    }

    /// <summary>
    /// Manager phê duyệt hoạt động (PENDING_APPROVAL -> APPROVED)
    /// </summary>
    public async Task<ServiceResult> ApproveActivityAsync(int activityId, int managerUserId, string? reviewNote)
    {
        var activity = await _context.Activities.FindAsync(activityId);
        if (activity == null)
        {
            return ServiceResult.Failed("Không tìm thấy hoạt động cần duyệt.");
        }

        if (activity.Status != ActivityStatus.PendingApproval)
        {
            return ServiceResult.Failed($"Chỉ hoạt động đang chờ duyệt (PENDING_APPROVAL) mới có thể phê duyệt. Trạng thái hiện tại: {activity.Status}");
        }

        activity.Status = ActivityStatus.Approved;
        activity.ReviewedByUserId = managerUserId;
        activity.ReviewedAt = DateTime.UtcNow;
        activity.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "Phê duyệt đủ điều kiện tổ chức." : reviewNote.Trim();
        activity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Manager {ManagerId} đã phê duyệt hoạt động {ActivityId}.", managerUserId, activityId);

        return ServiceResult.Ok("Đã phê duyệt hoạt động thành công.");
    }

    /// <summary>
    /// Manager từ chối hoạt động kèm lý do bắt buộc (PENDING_APPROVAL -> REJECTED)
    /// </summary>
    public async Task<ServiceResult> RejectActivityAsync(int activityId, int managerUserId, string reviewNote)
    {
        if (string.IsNullOrWhiteSpace(reviewNote))
        {
            return ServiceResult.Failed("Vui lòng nhập lý do từ chối để Ban tổ chức có căn cứ điều chỉnh.");
        }

        var activity = await _context.Activities.FindAsync(activityId);
        if (activity == null)
        {
            return ServiceResult.Failed("Không tìm thấy hoạt động cần từ chối.");
        }

        if (activity.Status != ActivityStatus.PendingApproval)
        {
            return ServiceResult.Failed($"Chỉ hoạt động đang chờ duyệt (PENDING_APPROVAL) mới có thể từ chối. Trạng thái hiện tại: {activity.Status}");
        }

        activity.Status = ActivityStatus.Rejected;
        activity.ReviewedByUserId = managerUserId;
        activity.ReviewedAt = DateTime.UtcNow;
        activity.ReviewNote = reviewNote.Trim();
        activity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Manager {ManagerId} đã từ chối hoạt động {ActivityId} với lý do: {Reason}", managerUserId, activityId, reviewNote);

        return ServiceResult.Ok("Đã từ chối hoạt động và lưu lại ý kiến giải trình.");
    }
}
