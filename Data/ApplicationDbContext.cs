using EMS_MVC.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EMS_MVC.Data;

/// <summary>
/// DbContext chính của ứng dụng EMS-MVC, ánh xạ 1:1 với schema SQL Server QLyDRL
/// Trách nhiệm chính: Ngô Trọng Thiện; Review & phối hợp: Lê Thành Hy
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users => Set<User>();
    public virtual DbSet<Role> Roles => Set<Role>();
    public virtual DbSet<UserRole> UserRoles => Set<UserRole>();
    public virtual DbSet<Department> Departments => Set<Department>();
    public virtual DbSet<Class> Classes => Set<Class>();
    public virtual DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public virtual DbSet<Semester> Semesters => Set<Semester>();
    public virtual DbSet<Student> Students => Set<Student>();
    public virtual DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public virtual DbSet<Activity> Activities => Set<Activity>();
    public virtual DbSet<ActivityRegistration> ActivityRegistrations => Set<ActivityRegistration>();
    public virtual DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public virtual DbSet<Attendance> Attendances => Set<Attendance>();
    public virtual DbSet<PointRule> PointRules => Set<PointRule>();
    public virtual DbSet<PointAdjustment> PointAdjustments => Set<PointAdjustment>();
    public virtual DbSet<PointAdjustmentApproval> PointAdjustmentApprovals => Set<PointAdjustmentApproval>();
    public virtual DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public virtual DbSet<Achievement> Achievements => Set<Achievement>();
    public virtual DbSet<StudentAchievement> StudentAchievements => Set<StudentAchievement>();
    public virtual DbSet<Notification> Notifications => Set<Notification>();
    public virtual DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo");
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.NormalizedUsername).IsUnique();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 2. Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "dbo");
            entity.HasKey(e => e.RoleId);
            entity.HasIndex(e => e.RoleName).IsUnique();
        });

        // 3. UserRoles (Many-to-Many composite PK)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles", "dbo");
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 4. Departments
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments", "dbo");
            entity.HasKey(e => e.DepartmentId);
            entity.HasIndex(e => e.DepartmentCode).IsUnique();
            entity.HasIndex(e => e.DepartmentName).IsUnique();
        });

        // 5. Classes
        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("Classes", "dbo");
            entity.HasKey(e => e.ClassId);
            entity.HasIndex(e => e.ClassCode).IsUnique();

            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Classes)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 6. AcademicYears
        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.ToTable("AcademicYears", "dbo");
            entity.HasKey(e => e.AcademicYearId);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // 7. Semesters
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.ToTable("Semesters", "dbo");
            entity.HasKey(e => e.SemesterId);
            entity.HasIndex(e => new { e.AcademicYearId, e.Name }).IsUnique();

            entity.HasOne(e => e.AcademicYear)
                  .WithMany(a => a.Semesters)
                  .HasForeignKey(e => e.AcademicYearId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. Students
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students", "dbo");
            entity.HasKey(e => e.StudentId);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.StudentCode).IsUnique();

            entity.HasOne(e => e.User)
                  .WithOne(u => u.StudentProfile)
                  .HasForeignKey<Student>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Class)
                  .WithMany(c => c.Students)
                  .HasForeignKey(e => e.ClassId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. ActivityTypes
        modelBuilder.Entity<ActivityType>(entity =>
        {
            entity.ToTable("ActivityTypes", "dbo");
            entity.HasKey(e => e.ActivityTypeId);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // 10. Activities
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("Activities", "dbo");
            entity.HasKey(e => e.ActivityId);
            entity.HasIndex(e => e.ActivityCode).IsUnique().HasFilter("[ActivityCode] IS NOT NULL");
            entity.HasIndex(e => new { e.Status, e.StartAt });

            entity.HasOne(e => e.ActivityType)
                  .WithMany(t => t.Activities)
                  .HasForeignKey(e => e.ActivityTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Organizer)
                  .WithMany(u => u.OrganizedActivities)
                  .HasForeignKey(e => e.OrganizerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Semester)
                  .WithMany(s => s.Activities)
                  .HasForeignKey(e => e.SemesterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReviewedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ReviewedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CancelledByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CancelledByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. ActivityRegistrations
        modelBuilder.Entity<ActivityRegistration>(entity =>
        {
            entity.ToTable("ActivityRegistrations", "dbo");
            entity.HasKey(e => e.RegistrationId);

            // BR01: Chống đăng ký trùng
            entity.HasIndex(e => new { e.ActivityId, e.StudentId }).IsUnique();

            // Composite alternate key phục vụ composite FK của Attendances
            entity.HasAlternateKey(e => new { e.RegistrationId, e.ActivityId });

            entity.HasOne(e => e.Activity)
                  .WithMany(a => a.Registrations)
                  .HasForeignKey(e => e.ActivityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.Registrations)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 12. AttendanceSessions
        modelBuilder.Entity<AttendanceSession>(entity =>
        {
            entity.ToTable("AttendanceSessions", "dbo");
            entity.HasKey(e => e.SessionId);

            // Composite alternate key phục vụ composite FK của Attendances
            entity.HasAlternateKey(e => new { e.SessionId, e.ActivityId });

            entity.HasIndex(e => e.QrTokenHash).IsUnique().HasFilter("[QrTokenHash] IS NOT NULL");

            entity.HasOne(e => e.Activity)
                  .WithMany(a => a.AttendanceSessions)
                  .HasForeignKey(e => e.ActivityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 13. Attendances
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("Attendances", "dbo");
            entity.HasKey(e => e.AttendanceId);

            // BR04: Duy nhất 1 bản ghi điểm danh cho mỗi session + registration
            entity.HasIndex(e => new { e.SessionId, e.RegistrationId }).IsUnique();

            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Attendances)
                  .HasPrincipalKey(s => new { s.SessionId, s.ActivityId })
                  .HasForeignKey(e => new { e.SessionId, e.ActivityId })
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Registration)
                  .WithMany(r => r.Attendances)
                  .HasPrincipalKey(r => new { r.RegistrationId, r.ActivityId })
                  .HasForeignKey(e => new { e.RegistrationId, e.ActivityId })
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RecordedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.RecordedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 14. PointRules
        modelBuilder.Entity<PointRule>(entity =>
        {
            entity.ToTable("PointRules", "dbo");
            entity.HasKey(e => e.PointRuleId);

            entity.HasOne(e => e.ActivityType)
                  .WithMany(t => t.PointRules)
                  .HasForeignKey(e => e.ActivityTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 15. PointAdjustments
        modelBuilder.Entity<PointAdjustment>(entity =>
        {
            entity.ToTable("PointAdjustments", "dbo");
            entity.HasKey(e => e.AdjustmentId);

            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Activity)
                  .WithMany()
                  .HasForeignKey(e => e.ActivityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RequestedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.RequestedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 16. PointAdjustmentApprovals
        modelBuilder.Entity<PointAdjustmentApproval>(entity =>
        {
            entity.ToTable("PointAdjustmentApprovals", "dbo");
            entity.HasKey(e => e.ApprovalId);

            entity.HasOne(e => e.Adjustment)
                  .WithMany(a => a.Approvals)
                  .HasForeignKey(e => e.AdjustmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ApprovedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ApprovedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 17. PointTransactions (Immutable ledger)
        modelBuilder.Entity<PointTransaction>(entity =>
        {
            entity.ToTable("PointTransactions", "dbo");
            entity.HasKey(e => e.TransactionId);

            // Chống cộng điểm hoạt động trùng
            entity.HasIndex(e => new { e.StudentId, e.ActivityId })
                  .IsUnique()
                  .HasFilter("[TransactionType] = 'ACTIVITY_REWARD' AND [ActivityId] IS NOT NULL");

            entity.HasIndex(e => e.PointAdjustmentId)
                  .IsUnique()
                  .HasFilter("[PointAdjustmentId] IS NOT NULL");

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.PointTransactions)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Activity)
                  .WithMany(a => a.PointTransactions)
                  .HasForeignKey(e => e.ActivityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PointRule)
                  .WithMany(r => r.PointTransactions)
                  .HasForeignKey(e => e.PointRuleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PointAdjustment)
                  .WithOne(a => a.PointTransaction)
                  .HasForeignKey<PointTransaction>(e => e.PointAdjustmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 18. Achievements
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.ToTable("Achievements", "dbo");
            entity.HasKey(e => e.AchievementId);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasOne(e => e.ActivityType)
                  .WithMany()
                  .HasForeignKey(e => e.ActivityTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 19. StudentAchievements
        modelBuilder.Entity<StudentAchievement>(entity =>
        {
            entity.ToTable("StudentAchievements", "dbo");
            entity.HasKey(e => new { e.StudentId, e.AchievementId });

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.Achievements)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Achievement)
                  .WithMany(a => a.StudentAchievements)
                  .HasForeignKey(e => e.AchievementId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.GrantedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.GrantedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 20. Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications", "dbo");
            entity.HasKey(e => e.NotificationId);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 21. AuditLogs
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs", "dbo");
            entity.HasKey(e => e.AuditLogId);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
