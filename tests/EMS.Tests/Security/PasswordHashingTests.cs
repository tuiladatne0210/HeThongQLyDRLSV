using EMS_MVC.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace EMS.Tests.Security;

/// <summary>
/// Kiểm thử tính an toàn của PasswordHasher: không lưu mật khẩu rõ, xác thực hash chính xác
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
public class PasswordHashingTests
{
    [Fact]
    public void PasswordHasher_ShouldNotProducePlainText_AndShouldVerifyCorrectly()
    {
        var hasher = new PasswordHasher<User>();
        var user = new User { Username = "testuser" };
        var rawPassword = "SecurePassword@123";

        // Act 1: Băm mật khẩu
        var hash = hasher.HashPassword(user, rawPassword);

        // Assert 1: Không chứa mật khẩu dạng rõ
        Assert.NotNull(hash);
        Assert.NotEqual(rawPassword, hash);
        Assert.DoesNotContain(rawPassword, hash);

        // Act & Assert 2: Xác thực mật khẩu đúng
        var successResult = hasher.VerifyHashedPassword(user, hash, rawPassword);
        Assert.Equal(PasswordVerificationResult.Success, successResult);

        // Act & Assert 3: Xác thực mật khẩu sai -> Thất bại
        var failResult = hasher.VerifyHashedPassword(user, hash, "WrongPassword@999");
        Assert.Equal(PasswordVerificationResult.Failed, failResult);
    }
}
