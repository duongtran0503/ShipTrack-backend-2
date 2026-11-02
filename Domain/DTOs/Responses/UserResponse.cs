namespace Logistics.Api.Domain.DTOs.Responses
{
    /// <summary>
    /// Đại diện cho cấu trúc dữ liệu người dùng (User) trả về cho phía Client sau khi đăng nhập, 
    /// xem hồ sơ hoặc cập nhật thông tin cá nhân.
    /// </summary>
    public record UserResponse(
        int Id,                  // Mã định danh người dùng
        int UserId,              // Bản sao mã định danh (hỗ trợ tương thích ngược với payload cũ)
        string Name,             // Họ và tên người dùng
        string Email,            // Địa chỉ email đăng nhập
        string Role,             // Vai trò hệ thống (Customer, Admin, DriverExpress,...)
        string Phone,            // Số điện thoại liên lạc
        string? LicensePlate,    // Biển số xe (đối với tài xế)
        decimal Balance,         // Số dư ví hiện tại
        bool IsActive,           // Trạng thái tài khoản (Đang hoạt động hay bị khóa)
        string? BanReason,       // Lý do khóa tài khoản (nếu có)
        bool Online              // Trạng thái trực tuyến nhận đơn của tài xế
    );
}
