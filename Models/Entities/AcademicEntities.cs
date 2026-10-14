using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.Models.Entities;

/// <summary>
/// Khoa / Đơn vị (dbo.Departments)
/// </summary>
public class Department
{
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(30)]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DepartmentName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}

/// <summary>
/// Lớp sinh hoạt (dbo.Classes)
/// </summary>
public class Class
{
    public int ClassId { get; set; }
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(30)]
    public string ClassCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ClassName { get; set; } = string.Empty;

    public short? CohortYear { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Department Department { get; set; } = null!;
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}

/// <summary>
/// Năm học (dbo.AcademicYears)
/// </summary>
public class AcademicYear
{
    public int AcademicYearId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();
}

/// <summary>
/// Học kỳ (dbo.Semesters)
/// </summary>
public class Semester
{
    public int SemesterId { get; set; }
    public int AcademicYearId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual AcademicYear AcademicYear { get; set; } = null!;
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

/// <summary>
/// Hồ sơ sinh viên (dbo.Students)
/// </summary>
public class Student
{
    public int StudentId { get; set; }
    public int UserId { get; set; }

    [Required]
    [MaxLength(30)]
    public string StudentCode { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }
    public int ClassId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Class Class { get; set; } = null!;
    public virtual ICollection<ActivityRegistration> Registrations { get; set; } = new List<ActivityRegistration>();
    public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
    public virtual ICollection<StudentAchievement> Achievements { get; set; } = new List<StudentAchievement>();
}
