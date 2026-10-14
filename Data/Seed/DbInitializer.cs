using EMS_MVC.Models.Entities;
using EMS_MVC.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Data.Seed;

/// <summary>
/// Khởi tạo dữ liệu mẫu và tài khoản phát triển an toàn
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        // 1. Roles
        var roles = new[]
        {
            new Role { RoleName = SystemRoles.Student, Description = "Sinh viên: đăng ký hoạt động và theo dõi điểm, thành tích.", IsSystemRole = true },
            new Role { RoleName = SystemRoles.Organizer, Description = "Ban tổ chức: quản lý hoạt động, đăng ký, điểm danh và yêu cầu điều chỉnh điểm.", IsSystemRole = true },
            new Role { RoleName = SystemRoles.Manager, Description = "Cán bộ quản lý: phê duyệt hoạt động, điều chỉnh điểm và xem báo cáo.", IsSystemRole = true },
            new Role { RoleName = SystemRoles.Admin, Description = "Quản trị viên: quản lý tài khoản, phân quyền và danh mục hệ thống.", IsSystemRole = true }
        };

        foreach (var role in roles)
        {
            if (!await context.Roles.AnyAsync(r => r.RoleName == role.RoleName))
            {
                context.Roles.Add(role);
            }
        }
        await context.SaveChangesAsync();

        // 2. Departments
        if (!await context.Departments.AnyAsync())
        {
            var depts = new[]
            {
                new Department { DepartmentCode = "CNTT", DepartmentName = "Khoa Công nghệ Thông tin", Description = "Đào tạo kỹ sư CNTT, KHMT, KTPM" },
                new Department { DepartmentCode = "QTKD", DepartmentName = "Khoa Quản trị Kinh doanh", Description = "Đào tạo cử nhân Quản trị Kinh doanh" },
                new Department { DepartmentCode = "NN", DepartmentName = "Khoa Ngoại ngữ", Description = "Đào tạo cử nhân Ngôn ngữ Anh, Nhật, Hàn" }
            };
            context.Departments.AddRange(depts);
            await context.SaveChangesAsync();
        }

        // 3. Classes
        if (!await context.Classes.AnyAsync())
        {
            var deptCntt = await context.Departments.FirstAsync(d => d.DepartmentCode == "CNTT");
            var deptQtkd = await context.Departments.FirstAsync(d => d.DepartmentCode == "QTKD");

            var classes = new[]
            {
                new Class { ClassCode = "21DTH01", ClassName = "Đại học CNTT K21 - Lớp 01", CohortYear = 2021, DepartmentId = deptCntt.DepartmentId },
                new Class { ClassCode = "22DTH02", ClassName = "Đại học CNTT K22 - Lớp 02", CohortYear = 2022, DepartmentId = deptCntt.DepartmentId },
                new Class { ClassCode = "21DQT01", ClassName = "Đại học Quản trị K21 - Lớp 01", CohortYear = 2021, DepartmentId = deptQtkd.DepartmentId }
            };
            context.Classes.AddRange(classes);
            await context.SaveChangesAsync();
        }

        // 4. AcademicYears & Semesters
        if (!await context.AcademicYears.AnyAsync())
        {
            var academicYear = new AcademicYear
            {
                Name = "2024-2025",
                StartDate = new DateOnly(2024, 9, 1),
                EndDate = new DateOnly(2025, 6, 30),
                IsActive = true
            };
            context.AcademicYears.Add(academicYear);
            await context.SaveChangesAsync();

            var semesters = new[]
            {
                new Semester
                {
                    AcademicYearId = academicYear.AcademicYearId,
                    Name = "Học kỳ 1 (2024 - 2025)",
                    StartDate = new DateOnly(2024, 9, 1),
                    EndDate = new DateOnly(2025, 1, 15),
                    IsActive = false
                },
                new Semester
                {
                    AcademicYearId = academicYear.AcademicYearId,
                    Name = "Học kỳ 2 (2024 - 2025)",
                    StartDate = new DateOnly(2025, 2, 1),
                    EndDate = new DateOnly(2025, 6, 30),
                    IsActive = true
                }
            };
            context.Semesters.AddRange(semesters);
            await context.SaveChangesAsync();
        }

        // 5. ActivityTypes
        if (!await context.ActivityTypes.AnyAsync())
        {
            var types = new[]
            {
                new ActivityType { Name = "Học thuật & Nghiên cứu khoa học", Description = "Hội thảo chuyên đề, tọa đàm công nghệ, nghiên cứu NCKH" },
                new ActivityType { Name = "Tình nguyện & Hoạt động xã hội", Description = "Mùa hè xanh, hiến máu tình nguyện, hỗ trợ cộng đồng" },
                new ActivityType { Name = "Thể dục thể thao & Thể chất", Description = "Hội thao sinh viên, giải bóng đá, chạy việt dã" },
                new ActivityType { Name = "Văn hóa nghệ thuật & Kỹ năng mềm", Description = "Giao lưu văn nghệ, kỹ năng giao tiếp, câu lạc bộ" }
            };
            context.ActivityTypes.AddRange(types);
            await context.SaveChangesAsync();
        }

        // 6. Users & Roles (Dev seed accounts)
        var roleStudent = await context.Roles.FirstAsync(r => r.RoleName == SystemRoles.Student);
        var roleOrganizer = await context.Roles.FirstAsync(r => r.RoleName == SystemRoles.Organizer);
        var roleManager = await context.Roles.FirstAsync(r => r.RoleName == SystemRoles.Manager);
        var roleAdmin = await context.Roles.FirstAsync(r => r.RoleName == SystemRoles.Admin);
        var classCntt = await context.Classes.FirstAsync(c => c.ClassCode == "21DTH01");
        var classCntt2 = await context.Classes.FirstAsync(c => c.ClassCode == "22DTH02");

        // Admin Account
        if (!await context.Users.AnyAsync(u => u.NormalizedUsername == "ADMIN"))
        {
            var adminUser = new User
            {
                Username = "admin",
                NormalizedUsername = "ADMIN",
                FullName = "Quản trị viên Hệ thống",
                Email = "admin@ems.edu.vn",
                NormalizedEmail = "ADMIN@EMS.EDU.VN",
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@12345");
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = adminUser.UserId, RoleId = roleAdmin.RoleId });
            await context.SaveChangesAsync();
        }

        // Manager Account
        if (!await context.Users.AnyAsync(u => u.NormalizedUsername == "MANAGER"))
        {
            var mgrUser = new User
            {
                Username = "manager",
                NormalizedUsername = "MANAGER",
                FullName = "Cán bộ Quản lý Đoàn - Hội",
                Email = "manager@ems.edu.vn",
                NormalizedEmail = "MANAGER@EMS.EDU.VN",
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            mgrUser.PasswordHash = passwordHasher.HashPassword(mgrUser, "Manager@12345");
            context.Users.Add(mgrUser);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = mgrUser.UserId, RoleId = roleManager.RoleId });
            await context.SaveChangesAsync();
        }

        // Organizer Account
        if (!await context.Users.AnyAsync(u => u.NormalizedUsername == "ORGANIZER"))
        {
            var orgUser = new User
            {
                Username = "organizer",
                NormalizedUsername = "ORGANIZER",
                FullName = "Ban Tổ chức Hoạt động Sinh viên",
                Email = "organizer@ems.edu.vn",
                NormalizedEmail = "ORGANIZER@EMS.EDU.VN",
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            orgUser.PasswordHash = passwordHasher.HashPassword(orgUser, "Organizer@12345");
            context.Users.Add(orgUser);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = orgUser.UserId, RoleId = roleOrganizer.RoleId });
            await context.SaveChangesAsync();
        }

        // Active Student Account: SV210145 (Nguyễn Anh Tuấn)
        if (!await context.Users.AnyAsync(u => u.NormalizedUsername == "SV210145"))
        {
            var stuUser = new User
            {
                Username = "sv210145",
                NormalizedUsername = "SV210145",
                FullName = "Nguyễn Anh Tuấn",
                Email = "sv210145@ems.edu.vn",
                NormalizedEmail = "SV210145@EMS.EDU.VN",
                PhoneNumber = "0901234567",
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            stuUser.PasswordHash = passwordHasher.HashPassword(stuUser, "Student@12345");
            context.Users.Add(stuUser);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = stuUser.UserId, RoleId = roleStudent.RoleId });
            context.Students.Add(new Student
            {
                UserId = stuUser.UserId,
                StudentCode = "SV210145",
                DateOfBirth = new DateOnly(2003, 5, 15),
                ClassId = classCntt.ClassId,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // Pending Student Account: SV220200 (Trần Bảo Long)
        if (!await context.Users.AnyAsync(u => u.NormalizedUsername == "SV220200"))
        {
            var pendingUser = new User
            {
                Username = "sv220200",
                NormalizedUsername = "SV220200",
                FullName = "Trần Bảo Long",
                Email = "sv220200@ems.edu.vn",
                NormalizedEmail = "SV220200@EMS.EDU.VN",
                PhoneNumber = "0912345678",
                Status = UserStatus.PendingApproval,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };
            pendingUser.PasswordHash = passwordHasher.HashPassword(pendingUser, "Student@12345");
            context.Users.Add(pendingUser);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = pendingUser.UserId, RoleId = roleStudent.RoleId });
            context.Students.Add(new Student
            {
                UserId = pendingUser.UserId,
                StudentCode = "SV220200",
                DateOfBirth = new DateOnly(2004, 8, 20),
                ClassId = classCntt2.ClassId,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // 7. Seed Sample Activities
        if (!await context.Activities.AnyAsync())
        {
            var org = await context.Users.FirstAsync(u => u.NormalizedUsername == "ORGANIZER");
            var semester = await context.Semesters.FirstAsync(s => s.IsActive);
            var typeAcademic = await context.ActivityTypes.FirstAsync(t => t.Name.Contains("Học thuật"));
            var typeVolunteer = await context.ActivityTypes.FirstAsync(t => t.Name.Contains("Tình nguyện"));
            var typeSkill = await context.ActivityTypes.FirstAsync(t => t.Name.Contains("Kỹ năng"));

            var now = DateTime.UtcNow;

            var act1 = new Activity
            {
                ActivityCode = "ACT-2024-001",
                Name = "Hội thảo Trí tuệ Nhân tạo và Xu hướng Điện toán Đám mây 2025",
                Description = "Hội thảo chia sẻ chuyên sâu về Generative AI, MLOps và các cơ hội nghề nghiệp trong ngành phần mềm cùng các diễn giả hàng đầu.",
                ActivityTypeId = typeAcademic.ActivityTypeId,
                OrganizerId = org.UserId,
                SemesterId = semester.SemesterId,
                Location = "Hội trường lớn Khu A - Tầng 2",
                StartAt = now.AddDays(7),
                EndAt = now.AddDays(7).AddHours(4),
                RegistrationStartAt = now.AddDays(-2),
                RegistrationEndAt = now.AddDays(5),
                MaxParticipants = 120,
                PointOverride = 10,
                AttendanceMode = AttendanceMode.CheckInOnly,
                Status = ActivityStatus.OpenRegistration,
                CreatedAt = now
            };

            var act2 = new Activity
            {
                ActivityCode = "ACT-2024-002",
                Name = "Chiến dịch Tình nguyện Tiếp sức Mùa thi & Hoa phượng đỏ",
                Description = "Hoạt động tình nguyện hỗ trợ thí sinh và phụ huynh tại các điểm thi tốt nghiệp THPT trên địa bàn TP.HCM.",
                ActivityTypeId = typeVolunteer.ActivityTypeId,
                OrganizerId = org.UserId,
                SemesterId = semester.SemesterId,
                Location = "Các điểm thi khu vực Quận 1, Quận 5 và Bình Thạnh",
                StartAt = now.AddDays(14),
                EndAt = now.AddDays(16),
                RegistrationStartAt = now.AddDays(-1),
                RegistrationEndAt = now.AddDays(10),
                MaxParticipants = 80,
                PointOverride = 15,
                AttendanceMode = AttendanceMode.CheckInCheckOut,
                Status = ActivityStatus.PendingApproval,
                CreatedAt = now
            };

            var act3 = new Activity
            {
                ActivityCode = "ACT-2024-003",
                Name = "Tập huấn Kỹ năng Thuyết trình Chuyên nghiệp & Quản lý Thời gian",
                Description = "Chương trình huấn luyện kỹ năng mềm dành cho sinh viên chuẩn bị báo cáo đồ án tốt nghiệp và phỏng vấn doanh nghiệp.",
                ActivityTypeId = typeSkill.ActivityTypeId,
                OrganizerId = org.UserId,
                SemesterId = semester.SemesterId,
                Location = "Phòng hội thảo B2-301",
                StartAt = now.AddDays(20),
                EndAt = now.AddDays(20).AddHours(3),
                RegistrationStartAt = now.AddDays(5),
                RegistrationEndAt = now.AddDays(18),
                MaxParticipants = 50,
                PointOverride = 8,
                AttendanceMode = AttendanceMode.CheckInOnly,
                Status = ActivityStatus.Draft,
                CreatedAt = now
            };

            context.Activities.AddRange(act1, act2, act3);
            await context.SaveChangesAsync();
        }

        // 8. Seed Sample Registration & Attendance Session for Immediate Demo
        if (!await context.AttendanceSessions.AnyAsync())
        {
            var act1 = await context.Activities.FirstOrDefaultAsync(a => a.ActivityCode == "ACT-2024-001");
            var stu = await context.Students.FirstOrDefaultAsync(s => s.StudentCode == "SV210145");
            var org = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == "ORGANIZER");

            if (act1 != null && org != null)
            {
                if (stu != null && !await context.ActivityRegistrations.AnyAsync(r => r.ActivityId == act1.ActivityId && r.StudentId == stu.StudentId))
                {
                    context.ActivityRegistrations.Add(new ActivityRegistration
                    {
                        ActivityId = act1.ActivityId,
                        StudentId = stu.StudentId,
                        RegisteredAt = DateTime.UtcNow.AddHours(-2),
                        Status = RegistrationStatus.Registered
                    });
                    await context.SaveChangesAsync();
                }

                var session = new AttendanceSession
                {
                    ActivityId = act1.ActivityId,
                    SessionName = "Phiên điểm danh đầu giờ - Check-in",
                    StartsAt = DateTime.UtcNow.AddMinutes(-30),
                    EndsAt = DateTime.UtcNow.AddHours(2),
                    Status = SessionStatus.Open,
                    CreatedByUserId = org.UserId,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                };
                context.AttendanceSessions.Add(session);
                await context.SaveChangesAsync();
            }
        }
    }
}
