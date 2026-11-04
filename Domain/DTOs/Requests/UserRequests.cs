using Logistics.Api.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.DTOs.Requests
{
    /// <summary>
    /// DTO dành cho Admin cập nhật thông tin người dùng.
    /// Không bao gồm PasswordHash — dùng ChangePasswordRequest cho việc đổi mật khẩu.
    /// </summary>
    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [StringLength(120, ErrorMessage = "Họ và tên không được vượt quá 120 ký tự.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vai trò người dùng không được để trống.")]
        [StringLength(20, ErrorMessage = "Vai trò không hợp lệ.")]
        [RegularExpression(
            @"^(CUSTOMER|ADMIN|DRIVER_EXPRESS|DRIVER_TRUCK|DRIVER_CONTAINER)$",
            ErrorMessage = "Vai trò không hợp lệ. Chỉ chấp nhận: CUSTOMER, ADMIN, DRIVER_EXPRESS, DRIVER_TRUCK, DRIVER_CONTAINER.")]
        public string Role { get; set; } = UserRole.Customer;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(30, ErrorMessage = "Số điện thoại tối đa 30 ký tự.")]
        public string Phone { get; set; } = "";

        [StringLength(30, ErrorMessage = "Biển số xe tối đa 30 ký tự.")]
        public string? LicensePlate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số dư không được âm.")]
        public decimal Balance { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(400, ErrorMessage = "Lý do khóa tài khoản tối đa 400 ký tự.")]
        public string? BanReason { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu đổi mật khẩu (dành cho user tự đổi).
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống.")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        [MaxLength(100, ErrorMessage = "Mật khẩu mới tối đa 100 ký tự.")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
