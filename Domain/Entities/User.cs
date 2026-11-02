using Logistics.Api.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

public class User
{
    public int Id { get; set; } // Mã định danh người dùng

    [MaxLength(120)]
    public string Name { get; set; } = ""; // Họ và tên

    [MaxLength(200)]
    public string Email { get; set; } = ""; // Địa chỉ email đăng nhập

    public string PasswordHash { get; set; } = ""; // Mật khẩu đã mã hóa

    [MaxLength(20)]
    public string Role { get; set; } = UserRole.Customer; // Vai trò mặc định là customer

    [MaxLength(30)]
    public string Phone { get; set; } = ""; // Số điện thoại liên hệ

    [MaxLength(30)]
    public string LicensePlate { get; set; } = ""; // Biển số xe (dành cho tài xế)

    public decimal Balance { get; set; } // Số dư ví tiền (VND)

    public bool IsActive { get; set; } = true; // Trạng thái tài khoản (true: hoạt động, false: bị khóa)

    [MaxLength(400)]
    public string BanReason { get; set; } = ""; // Lý do bị khóa tài khoản

    public bool Online { get; set; } // Trạng thái sẵn sàng nhận đơn của tài xế

    public double? Latitude { get; set; } // Vĩ độ hiện tại (GPS tài xế)

    public double? Longitude { get; set; } // Kinh độ hiện tại (GPS tài xế)

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // Thời điểm tạo tài khoản (UTC)
}