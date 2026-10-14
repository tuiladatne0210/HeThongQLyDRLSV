using EMS_MVC.Data;
using EMS_MVC.Data.Seed;
using EMS_MVC.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EMS.Tests.Database;

/// <summary>
/// Kiểm thử kết nối SQL Server thật (QLyDRL) và chạy DbInitializer để seed dữ liệu phát triển an toàn
/// Trách nhiệm chính: Ngô Trọng Thiện
/// </summary>
public class SqlDatabaseSeedIntegrationTests
{
    private const string ConnectionString = "Server=localhost;Database=QLyDRL;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    [Fact]
    public async Task CanConnectToSqlServer_AndSeedMasterDataSuccessfully()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var db = new ApplicationDbContext(options);

        // Kiểm tra xem có thể kết nối được tới cơ sở dữ liệu không
        bool canConnect = await db.Database.CanConnectAsync();
        Assert.True(canConnect, "Không thể kết nối tới cơ sở dữ liệu SQL Server QLyDRL trên localhost.");

        var hasher = new PasswordHasher<User>();

        // Thực thi seed dữ liệu an toàn
        await DbInitializer.SeedAsync(db, hasher);

        // Kiểm tra số lượng người dùng sau khi seed
        var userCount = await db.Users.CountAsync();
        Assert.True(userCount >= 4, "Số lượng Users sau khi seed phải tối thiểu là 4 tài khoản (Admin, Manager, Organizer, Student).");

        // Kiểm tra số lượng hoạt động sau khi seed
        var activityCount = await db.Activities.CountAsync();
        Assert.True(activityCount >= 3, "Số lượng Activities sau khi seed phải tối thiểu là 3 hoạt động mẫu.");
    }
}
