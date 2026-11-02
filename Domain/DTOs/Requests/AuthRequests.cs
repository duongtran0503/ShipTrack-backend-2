using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.DTOs.Requests;

/// <summary>
/// DTO yêu cầu đăng nhập hệ thống.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string Password { get; set; } = "";
}

/// <summary>
/// DTO yêu cầu đăng ký tài khoản mới.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [StringLength(120, ErrorMessage = "Họ và tên không được vượt quá 120 ký tự.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vai trò người dùng không được để trống.")]
    [StringLength(20, ErrorMessage = "Vai trò không hợp lệ.")]
    public string Role { get; set; } = "";

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(30, ErrorMessage = "Số điện thoại tối đa 30 ký tự.")]
    public string Phone { get; set; } = "";

    [StringLength(30, ErrorMessage = "Biển số xe tối đa 30 ký tự.")]
    public string? LicensePlate { get; set; }
}

/// <summary>
/// DTO chứa thông tin cập nhật hồ sơ cá nhân.
/// </summary>
public class ProfileRequest
{
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [StringLength(120, ErrorMessage = "Họ và tên không được vượt quá 120 ký tự.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(30, ErrorMessage = "Số điện thoại tối đa 30 ký tự.")]
    public string Phone { get; set; } = "";

    [StringLength(30, ErrorMessage = "Biển số xe tối đa 30 ký tự.")]
    public string? LicensePlate { get; set; }
}

/// <summary>
/// DTO dành cho Admin quản lý khóa hoặc mở khóa tài khoản tài xế.
/// </summary>
public class DriverAccountRequest
{
    [Required(ErrorMessage = "Trạng thái kích hoạt là bắt buộc.")]
    public bool IsActive { get; set; }

    [StringLength(400, ErrorMessage = "Lý do khóa tài khoản tối đa 400 ký tự.")]
    public string? Reason { get; set; }
}