using EMS_MVC.Data;
using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using EMS_MVC.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EMS.Tests.BusinessRules;

/// <summary>
/// Kiểm thử các quy tắc nghiệp vụ Đăng ký hoạt động: BR01, BR02, BR03
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
public class RegistrationRulesTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task BR01_DuplicateRegistration_ShouldBeRejected()
    {
        // Arrange: Tạo DB và dữ liệu hoạt động mở đăng ký
        var db = CreateInMemoryDbContext(nameof(BR01_DuplicateRegistration_ShouldBeRejected));
        var logger = NullLogger<RegistrationService>.Instance;
        var service = new RegistrationService(db, logger);

        var now = DateTime.UtcNow;
        var activity = new Activity
        {
            Name = "Hoạt động thử nghiệm BR01",
            Status = ActivityStatus.OpenRegistration,
            RegistrationStartAt = now.AddDays(-1),
            RegistrationEndAt = now.AddDays(2),
            StartAt = now.AddDays(3),
            EndAt = now.AddDays(4),
            MaxParticipants = 50,
            Location = "Phòng A1"
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        int studentId = 101;

        // Act 1: Đăng ký lần đầu -> Thành công
        var firstResult = await service.RegisterAsync(activity.ActivityId, studentId);
        Assert.True(firstResult.Success);

        // Act 2: Đăng ký lần 2 với cùng StudentId và ActivityId -> Bị từ chối theo BR01
        var secondResult = await service.RegisterAsync(activity.ActivityId, studentId);

        // Assert:
        Assert.False(secondResult.Success);
        Assert.Contains("Không được đăng ký trùng", secondResult.Message);
    }

    [Fact]
    public async Task BR02_RegistrationOutsideTimeWindow_ShouldBeRejected()
    {
        // Arrange: Hoạt động đã hết hạn đăng ký
        var db = CreateInMemoryDbContext(nameof(BR02_RegistrationOutsideTimeWindow_ShouldBeRejected));
        var logger = NullLogger<RegistrationService>.Instance;
        var service = new RegistrationService(db, logger);

        var now = DateTime.UtcNow;
        var expiredActivity = new Activity
        {
            Name = "Hoạt động đã hết hạn đăng ký",
            Status = ActivityStatus.OpenRegistration,
            RegistrationStartAt = now.AddDays(-5),
            RegistrationEndAt = now.AddDays(-1), // Đã hết hạn hôm qua
            StartAt = now.AddDays(2),
            EndAt = now.AddDays(3),
            MaxParticipants = 50,
            Location = "Sân bóng"
        };
        db.Activities.Add(expiredActivity);
        await db.SaveChangesAsync();

        // Act: Sinh viên cố gắng đăng ký ngoài khung giờ
        var result = await service.RegisterAsync(expiredActivity.ActivityId, studentId: 102);

        // Assert: Bị từ chối theo BR02
        Assert.False(result.Success);
        Assert.Contains("Đã hết thời gian nhận đăng ký", result.Message);
    }

    [Fact]
    public async Task BR03_OvercapacityRegistration_ShouldBeRejected()
    {
        // Arrange: Hoạt động chỉ có sức chứa tối đa 1 người
        var db = CreateInMemoryDbContext(nameof(BR03_OvercapacityRegistration_ShouldBeRejected));
        var logger = NullLogger<RegistrationService>.Instance;
        var service = new RegistrationService(db, logger);

        var now = DateTime.UtcNow;
        var fullActivity = new Activity
        {
            Name = "Hoạt động giới hạn 1 chỗ duy nhất",
            Status = ActivityStatus.OpenRegistration,
            RegistrationStartAt = now.AddDays(-1),
            RegistrationEndAt = now.AddDays(2),
            StartAt = now.AddDays(3),
            EndAt = now.AddDays(4),
            MaxParticipants = 1, // Sức chứa tối đa 1
            Location = "Phòng họp nhỏ"
        };
        db.Activities.Add(fullActivity);
        await db.SaveChangesAsync();

        // Act 1: Sinh viên 1 đăng ký thành công (đạt 1/1)
        var res1 = await service.RegisterAsync(fullActivity.ActivityId, studentId: 201);
        Assert.True(res1.Success);

        // Act 2: Sinh viên 2 đăng ký -> Vượt sức chứa
        var res2 = await service.RegisterAsync(fullActivity.ActivityId, studentId: 202);

        // Assert: Bị từ chối theo BR03
        Assert.False(res2.Success);
        Assert.Contains("đủ số lượng đăng ký tối đa", res2.Message);
    }
}
