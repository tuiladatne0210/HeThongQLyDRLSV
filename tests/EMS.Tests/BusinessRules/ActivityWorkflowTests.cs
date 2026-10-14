using EMS_MVC.Data;
using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Implementations;
using EMS_MVC.ViewModels.Activity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EMS.Tests.BusinessRules;

/// <summary>
/// Kiểm thử quy trình chuyển trạng thái hoạt động: DRAFT -> PENDING_APPROVAL -> APPROVED / REJECTED -> OPEN_REGISTRATION
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
public class ActivityWorkflowTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateActivity_ShouldStartAtDraftStatus()
    {
        var db = CreateInMemoryDbContext(nameof(CreateActivity_ShouldStartAtDraftStatus));
        var service = new ActivityService(db, NullLogger<ActivityService>.Instance);

        var now = DateTime.UtcNow;
        var model = new ActivityCreateEditViewModel
        {
            Name = "Hoạt động DRAFT",
            Location = "Hội trường",
            ActivityTypeId = 1,
            SemesterId = 1,
            StartAt = now.AddDays(7),
            EndAt = now.AddDays(7).AddHours(2),
            RegistrationStartAt = now.AddDays(1),
            RegistrationEndAt = now.AddDays(5),
            AttendanceMode = AttendanceMode.CheckInOnly
        };

        var result = await service.CreateActivityAsync(model, organizerUserId: 1);

        Assert.True(result.Success);
        var created = await db.Activities.FindAsync(result.Data);
        Assert.NotNull(created);
        Assert.Equal(ActivityStatus.Draft, created.Status);
    }

    [Fact]
    public async Task SubmitForApproval_FromDraft_ShouldTransitionToPendingApproval()
    {
        var db = CreateInMemoryDbContext(nameof(SubmitForApproval_FromDraft_ShouldTransitionToPendingApproval));
        var service = new ActivityService(db, NullLogger<ActivityService>.Instance);

        var activity = new Activity
        {
            Name = "Hoạt động chờ gửi duyệt",
            Status = ActivityStatus.Draft,
            OrganizerId = 1,
            Location = "Online"
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        var result = await service.SubmitForApprovalAsync(activity.ActivityId, organizerUserId: 1);

        Assert.True(result.Success);
        var updated = await db.Activities.FindAsync(activity.ActivityId);
        Assert.Equal(ActivityStatus.PendingApproval, updated!.Status);
    }

    [Fact]
    public async Task ApproveActivity_ShouldChangeStatusToApproved()
    {
        var db = CreateInMemoryDbContext(nameof(ApproveActivity_ShouldChangeStatusToApproved));
        var service = new ActivityService(db, NullLogger<ActivityService>.Instance);

        var activity = new Activity
        {
            Name = "Hoạt động cần duyệt",
            Status = ActivityStatus.PendingApproval,
            OrganizerId = 1,
            Location = "Cơ sở 1"
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        var result = await service.ApproveActivityAsync(activity.ActivityId, managerUserId: 99, reviewNote: "Đủ điều kiện");

        Assert.True(result.Success);
        var updated = await db.Activities.FindAsync(activity.ActivityId);
        Assert.Equal(ActivityStatus.Approved, updated!.Status);
        Assert.Equal(99, updated.ReviewedByUserId);
        Assert.NotNull(updated.ReviewedAt);
    }

    [Fact]
    public async Task RejectActivity_WithoutReason_ShouldBeRejected()
    {
        var db = CreateInMemoryDbContext(nameof(RejectActivity_WithoutReason_ShouldBeRejected));
        var service = new ActivityService(db, NullLogger<ActivityService>.Instance);

        var activity = new Activity
        {
            Name = "Hoạt động thẩm định",
            Status = ActivityStatus.PendingApproval,
            OrganizerId = 1,
            Location = "Cơ sở 2"
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        // Từ chối không nhập lý do -> Bị hệ thống chặn
        var result = await service.RejectActivityAsync(activity.ActivityId, managerUserId: 99, reviewNote: "");

        Assert.False(result.Success);
        Assert.Contains("lý do", result.Message.ToLower());

        // Từ chối có lý do hợp lệ -> Thành công
        var validReject = await service.RejectActivityAsync(activity.ActivityId, managerUserId: 99, reviewNote: "Kế hoạch kinh phí chưa rõ ràng");
        Assert.True(validReject.Success);
        var updated = await db.Activities.FindAsync(activity.ActivityId);
        Assert.Equal(ActivityStatus.Rejected, updated!.Status);
    }

    [Fact]
    public async Task OpenRegistration_FromApproved_ShouldChangeToOpenRegistration()
    {
        var db = CreateInMemoryDbContext(nameof(OpenRegistration_FromApproved_ShouldChangeToOpenRegistration));
        var service = new ActivityService(db, NullLogger<ActivityService>.Instance);

        var activity = new Activity
        {
            Name = "Hoạt động đã duyệt chờ mở ĐK",
            Status = ActivityStatus.Approved,
            OrganizerId = 1,
            Location = "Hội trường A"
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        var result = await service.OpenRegistrationAsync(activity.ActivityId, organizerUserId: 1);

        Assert.True(result.Success);
        var updated = await db.Activities.FindAsync(activity.ActivityId);
        Assert.Equal(ActivityStatus.OpenRegistration, updated!.Status);
    }
}
