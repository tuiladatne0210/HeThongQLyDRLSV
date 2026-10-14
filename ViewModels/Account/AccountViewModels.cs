using System.ComponentModel.DataAnnotations;

namespace EMS_MVC.ViewModels.Account;

/// <summary>
/// ViewModel đăng nhập hệ thống
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập hoặc Mã sinh viên")]
    [Display(Name = "Tên đăng nhập / Mã sinh viên")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ phiên đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>
/// ViewModel đăng ký tài khoản sinh viên mới (chờ Admin duyệt)
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập từ 4 đến 50 ký tự")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Mã sinh viên")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "Mã sinh viên từ 5 đến 20 ký tự")]
    [Display(Name = "Mã sinh viên (MSSV)")]
    public string StudentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Họ và tên")]
    [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email sinh viên")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Lớp học")]
    [Display(Name = "Lớp sinh hoạt")]
    public int ClassId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel đổi mật khẩu
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới từ 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu mới")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
