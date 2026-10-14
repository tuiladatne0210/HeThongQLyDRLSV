using EMS_MVC.Data;
using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EMS.Tests.BusinessRules;

/// <summary>
/// Kiểm thử quy tắc điểm danh thủ công: BR04
/// Chỉ sinh viên có Registration hợp lệ mới được ghi nhận Attendance
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
public class AttendanceRulesTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task BR04_ManualAttendance_WithValidRegistration_ShouldSucceedAndMarkAttended()
    {
        var db = CreateInMemoryDbContext(nameof(BR04_ManualAttendance_WithValidRegistration_ShouldSucceedAndMarkAttended));
        var service = new AttendanceService(db, NullLogger<AttendanceService>.Instance);

        var now = DateTime.UtcNow;
        var activity = new Activity
        {
            Name = "Hoạt động thử nghiệm BR04",
            Status = ActivityStatus.Ongoing,
            Location = "Cơ sở 1"
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        var session = new AttendanceSession
        {
            ActivityId = activity.ActivityId,
            SessionName = "Phiên điểm danh sáng",
            Status = SessionStatus.Open,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddHours(2),
            CreatedByUserId = 1
        };
        db.AttendanceSessions.Add(session);

        var studentUser = new User { Username = "sv_test", FullName = "Sinh viên Kiểm thử" };
        db.Users.Add(studentUser);
        await db.SaveChangesAsync();

        var student = new Student { UserId = studentUser.UserId, StudentCode = "SV999", ClassId = 1 };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var registration = new ActivityRegistration
        {
            ActivityId = activity.ActivityId,
            StudentId = student.StudentId,
            Status = RegistrationStatus.Registered,
            RegisteredAt = now.AddDays(-1)
        };
        db.ActivityRegistrations.Add(registration);
        await db.SaveChangesAsync();

        // Act: Ghi nhận điểm danh thủ công cho sinh viên đã đăng ký hợp lệ
        var result = await service.CheckInManualAsync(session.SessionId, registration.RegistrationId, recordedByUserId: 1);

        // Assert: Thành công, tạo bản ghi Attendances với METHOD=MANUAL và cập nhật Registration thành ATTENDED
        Assert.True(result.Success);

        var att = await db.Attendances.FirstOrDefaultAsync(a => a.SessionId == session.SessionId && a.RegistrationId == registration.RegistrationId);
        Assert.NotNull(att);
        Assert.Equal(AttendanceMethod.Manual, att.Method);
        Assert.Equal(AttendanceRecordStatus.Valid, att.Status);

        var updatedReg = await db.ActivityRegistrations.FindAsync(registration.RegistrationId);
        Assert.Equal(RegistrationStatus.Attended, updatedReg!.Status);
    }

    [Fact]
    public async Task BR04_ManualAttendance_DuplicateInSameSession_ShouldBeRejected()
    {
        var db = CreateInMemoryDbContext(nameof(BR04_ManualAttendance_DuplicateInSameSession_ShouldBeRejected));
        var service = new AttendanceService(db, NullLogger<AttendanceService>.Instance);

        var now = DateTime.UtcNow;
        var activity = new Activity { Name = "Hoạt động", Status = ActivityStatus.Ongoing, Location = "Online" };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        var session = new AttendanceSession
        {
            ActivityId = activity.ActivityId,
            Status = SessionStatus.Open,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddHours(1),
            CreatedByUserId = 1
        };
        db.AttendanceSessions.Add(session);

        var studentUser = new User { Username = "sv_dup", FullName = "SV Trùng lặp" };
        db.Users.Add(studentUser);
        await db.SaveChangesAsync();

        var student = new Student { UserId = studentUser.UserId, StudentCode = "SV888", ClassId = 1 };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var registration = new ActivityRegistration
        {
            ActivityId = activity.ActivityId,
            StudentId = student.StudentId,
            Status = RegistrationStatus.Registered,
            RegisteredAt = now.AddDays(-1)
        };
        db.ActivityRegistrations.Add(registration);
        await db.SaveChangesAsync();

        // Lần 1: Điểm danh thành công
        var res1 = await service.CheckInManualAsync(session.SessionId, registration.RegistrationId, recordedByUserId: 1);
        Assert.True(res1.Success);

        // Lần 2: Thử điểm danh lại trên cùng phiên -> Bị từ chối
        var res2 = await service.CheckInManualAsync(session.SessionId, registration.RegistrationId, recordedByUserId: 1);
        Assert.False(res2.Success);
        Assert.Contains("đã được điểm danh trong phiên này trước đó", res2.Message);
    }

    [Fact]
    public async Task BR04_ManualAttendance_WhenSessionClosed_ShouldBeRejected()
    {
        var db = CreateInMemoryDbContext(nameof(BR04_ManualAttendance_WhenSessionClosed_ShouldBeRejected));
        var service = new AttendanceService(db, NullLogger<AttendanceService>.Instance);

        var activity = new Activity { Name = "Hoạt động", Status = ActivityStatus.Ongoing, Location = "Sân trường" };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        var closedSession = new AttendanceSession
        {
            ActivityId = activity.ActivityId,
            Status = SessionStatus.Closed, // Đã đóng
            StartsAt = DateTime.UtcNow.AddHours(-3),
            EndsAt = DateTime.UtcNow.AddHours(-1),
            CreatedByUserId = 1
        };
        db.AttendanceSessions.Add(closedSession);

        var reg = new ActivityRegistration
        {
            ActivityId = activity.ActivityId,
            StudentId = 1,
            Status = RegistrationStatus.Registered
        };
        db.ActivityRegistrations.Add(reg);
        await db.SaveChangesAsync();

        var result = await service.CheckInManualAsync(closedSession.SessionId, reg.RegistrationId, recordedByUserId: 1);
        Assert.False(result.Success);
        Assert.Contains("đã ĐÓNG", result.Message);
    }
}
