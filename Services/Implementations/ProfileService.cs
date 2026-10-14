using EMS_MVC.Data;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Common;
using EMS_MVC.Services.Interfaces;
using EMS_MVC.ViewModels.Student;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ hồ sơ và tổng hợp thông tin Dashboard cho sinh viên
/// Trách nhiệm chính: Lê Thành Đạt; Review & phối hợp: Lê Thành Hy
/// </summary>
public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;

    public ProfileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudentDashboardViewModel?> GetStudentDashboardAsync(int userId)
    {
        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Class)
                .ThenInclude(c => c.Department)
            .Include(s => s.Registrations)
                .ThenInclude(r => r.Activity)
                    .ThenInclude(a => a.ActivityType)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null) return null;

        var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive)
                              ?? await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

        // Tính tổng điểm từ sổ giao dịch PointTransactions (không lưu trường tổng điểm độc lập)
        var totalPoints = await _context.PointTransactions
            .Where(pt => pt.StudentId == student.StudentId)
            .SumAsync(pt => (long)pt.Points);

        var attendedCount = student.Registrations.Count(r => r.Status == RegistrationStatus.Attended);
        var upcomingRegistered = student.Registrations.Count(r => r.Status == RegistrationStatus.Registered && r.Activity.StartAt >= DateTime.UtcNow);

        string classification;
        if (totalPoints >= 90) classification = "Xuất sắc";
        else if (totalPoints >= 80) classification = "Tốt";
        else if (totalPoints >= 65) classification = "Khá";
        else if (totalPoints >= 50) classification = "Trung bình";
        else classification = "Yếu";

        // Danh sách hoạt động sắp diễn ra đã đăng ký
        var upcomingList = student.Registrations
            .Where(r => r.Status == RegistrationStatus.Registered && r.Activity.StartAt >= DateTime.UtcNow)
            .OrderBy(r => r.Activity.StartAt)
            .Take(4)
            .Select(r => new StudentActivityItemViewModel
            {
                ActivityId = r.Activity.ActivityId,
                ActivityCode = r.Activity.ActivityCode,
                Name = r.Activity.Name,
                ActivityTypeName = r.Activity.ActivityType.Name,
                Location = r.Activity.Location,
                StartAt = r.Activity.StartAt,
                EndAt = r.Activity.EndAt,
                Points = r.Activity.PointOverride ?? 5,
                Status = r.Activity.Status,
                RegistrationStatus = r.Status
            }).ToList();

        // Danh sách hoạt động đang mở đăng ký sinh viên chưa tham gia
        var openList = await _context.Activities
            .Include(a => a.ActivityType)
            .Include(a => a.Registrations)
            .Where(a => a.Status == ActivityStatus.OpenRegistration &&
                        !a.Registrations.Any(r => r.StudentId == student.StudentId && (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended)))
            .OrderBy(a => a.RegistrationEndAt)
            .Take(4)
            .Select(a => new StudentActivityItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityCode = a.ActivityCode,
                Name = a.Name,
                ActivityTypeName = a.ActivityType.Name,
                Location = a.Location,
                StartAt = a.StartAt,
                EndAt = a.EndAt,
                RegistrationEndAt = a.RegistrationEndAt,
                Points = a.PointOverride ?? 5,
                Status = a.Status,
                RegisteredCount = a.Registrations.Count(r => r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Attended),
                MaxParticipants = a.MaxParticipants
            }).ToListAsync();

        return new StudentDashboardViewModel
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.User.FullName,
            ClassName = student.Class.ClassName,
            DepartmentName = student.Class.Department.DepartmentName,
            SemesterName = currentSemester?.Name ?? "Học kỳ hiện tại",
            TotalPoints = totalPoints,
            AttendedActivitiesCount = attendedCount,
            RegisteredUpcomingCount = upcomingRegistered,
            Classification = classification,
            UpcomingActivities = upcomingList,
            OpenRegistrationActivities = openList
        };
    }

    public async Task<StudentProfileViewModel?> GetStudentProfileAsync(int userId)
    {
        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Class)
                .ThenInclude(c => c.Department)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null) return null;

        return new StudentProfileViewModel
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.User.FullName,
            Username = student.User.Username,
            Email = student.User.Email,
            PhoneNumber = student.User.PhoneNumber,
            DateOfBirth = student.DateOfBirth,
            ClassName = student.Class.ClassName,
            DepartmentName = student.Class.Department.DepartmentName,
            CreatedAt = student.CreatedAt
        };
    }

    public async Task<ServiceResult> UpdateStudentProfileAsync(int userId, string? email, string? phoneNumber)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ServiceResult.Failed("Không tìm thấy thông tin tài khoản người dùng.");
        }

        user.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        await _context.SaveChangesAsync();
        return ServiceResult.Ok("Cập nhật thông tin liên lạc thành công.");
    }

    public async Task<List<StudentActivityItemViewModel>> GetParticipationHistoryAsync(int userId)
    {
        var student = await _context.Students
            .Include(s => s.Registrations)
                .ThenInclude(r => r.Activity)
                    .ThenInclude(a => a.ActivityType)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null) return new();

        return student.Registrations
            .OrderByDescending(r => r.Activity.StartAt)
            .Select(r => new StudentActivityItemViewModel
            {
                ActivityId = r.Activity.ActivityId,
                ActivityCode = r.Activity.ActivityCode,
                Name = r.Activity.Name,
                ActivityTypeName = r.Activity.ActivityType.Name,
                Location = r.Activity.Location,
                StartAt = r.Activity.StartAt,
                EndAt = r.Activity.EndAt,
                Points = r.Activity.PointOverride ?? 5,
                Status = r.Activity.Status,
                RegistrationStatus = r.Status
            }).ToList();
    }

    public async Task<List<PointHistoryItemViewModel>> GetPointHistoryAsync(int userId)
    {
        var student = await _context.Students
            .Include(s => s.Registrations)
                .ThenInclude(r => r.Activity)
                    .ThenInclude(a => a.ActivityType)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null) return new();

        // 1. Thử lấy từ sổ giao dịch PointTransactions
        var transactions = await _context.PointTransactions
            .Include(pt => pt.Activity)
                .ThenInclude(a => a.ActivityType)
            .Where(pt => pt.StudentId == student.StudentId)
            .OrderByDescending(pt => pt.CreatedAt)
            .Select(pt => new PointHistoryItemViewModel
            {
                TransactionId = pt.TransactionId,
                ActivityName = pt.Activity != null ? pt.Activity.Name : "Điểm rèn luyện",
                ActivityTypeName = (pt.Activity != null && pt.Activity.ActivityType != null) ? pt.Activity.ActivityType.Name : "Quy chế tích lũy",
                Points = pt.Points,
                TransactionType = pt.TransactionType,
                Reason = pt.Description,
                CreatedAt = pt.CreatedAt
            }).ToListAsync();

        if (transactions.Any())
        {
            return transactions;
        }

        // 2. Nếu chưa sinh PointTransactions, tổng hợp từ các hoạt động sinh viên đã điểm danh có mặt (ATTENDED)
        return student.Registrations
            .Where(r => r.Status == RegistrationStatus.Attended)
            .OrderByDescending(r => r.Activity.StartAt)
            .Select(r => new PointHistoryItemViewModel
            {
                TransactionId = r.RegistrationId,
                ActivityName = r.Activity.Name,
                ActivityTypeName = r.Activity.ActivityType.Name,
                Points = r.Activity.PointOverride ?? 5,
                TransactionType = "EARNED",
                Reason = "Điểm danh hoàn thành tham gia sự kiện",
                CreatedAt = r.RegisteredAt
            }).ToList();
    }
}

