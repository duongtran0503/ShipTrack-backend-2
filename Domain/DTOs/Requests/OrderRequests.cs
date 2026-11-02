using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.DTOs.Requests;

/// <summary>
/// DTO chứa thông tin yêu cầu tạo đơn hàng mới từ phía Khách hàng.
/// </summary>
public record CreateOrderRequest(
    [Required(ErrorMessage = "Địa chỉ lấy hàng không được để trống.")]
    [StringLength(500, ErrorMessage = "Địa chỉ lấy hàng tối đa 500 ký tự.")]
    string PickupAddress,

    [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống.")]
    [StringLength(500, ErrorMessage = "Địa chỉ giao hàng tối đa 500 ký tự.")]
    string DropoffAddress,

    [Range(-90, 90, ErrorMessage = "Vĩ độ điểm lấy hàng (PickupLat) không hợp lệ.")]
    double PickupLat,

    [Range(-180, 180, ErrorMessage = "Kinh độ điểm lấy hàng (PickupLng) không hợp lệ.")]
    double PickupLng,

    [Range(-90, 90, ErrorMessage = "Vĩ độ điểm giao hàng (DropoffLat) không hợp lệ.")]
    double DropoffLat,

    [Range(-180, 180, ErrorMessage = "Kinh độ điểm giao hàng (DropoffLng) không hợp lệ.")]
    double DropoffLng,

    [Required(ErrorMessage = "Loại phương tiện không được để trống.")]
    [StringLength(25, ErrorMessage = "Loại phương tiện tối đa 25 ký tự.")]
    string VehicleType,

    [Required(ErrorMessage = "Loại dịch vụ không được để trống.")]
    [StringLength(25, ErrorMessage = "Loại dịch vụ tối đa 25 ký tự.")]
    string ServiceType,

    [Required(ErrorMessage = "Tên người gửi không được để trống.")]
    [StringLength(120, ErrorMessage = "Tên người gửi tối đa 120 ký tự.")]
    string SenderName,

    [Required(ErrorMessage = "Số điện thoại người gửi không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại người gửi không hợp lệ.")]
    [StringLength(30, ErrorMessage = "Số điện thoại người gửi tối đa 30 ký tự.")]
    string SenderPhone,

    [Required(ErrorMessage = "Tên người nhận không được để trống.")]
    [StringLength(120, ErrorMessage = "Tên người nhận tối đa 120 ký tự.")]
    string ReceiverName,

    [Required(ErrorMessage = "Số điện thoại người nhận không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại người nhận không hợp lệ.")]
    [StringLength(30, ErrorMessage = "Số điện thoại người nhận tối đa 30 ký tự.")]
    string ReceiverPhone,

    [Required(ErrorMessage = "Tên hàng hóa không được để trống.")]
    [StringLength(150, ErrorMessage = "Tên hàng hóa tối đa 150 ký tự.")]
    string PackageName,

    [Range(0.1, 50000, ErrorMessage = "Trọng lượng hàng hóa (WeightKg) phải lớn hơn 0.")]
    double WeightKg,

    [StringLength(1000, ErrorMessage = "Chi tiết gói hàng tối đa 1000 ký tự.")]
    string PackageDetails,

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? Note,

    [Required(ErrorMessage = "Phương thức thanh toán không được để trống.")]
    [StringLength(20, ErrorMessage = "Phương thức thanh toán tối đa 20 ký tự.")]
    string PaymentMethod,

    bool IsBulky,
    bool IsDoorDelivery,

    [Range(0, 100000000, ErrorMessage = "Tiền tip không hợp lệ.")]
    decimal TipAmount,

    [Range(0, 500000000, ErrorMessage = "Tiền thu hộ COD không hợp lệ.")]
    decimal CodAmount,

    [StringLength(32, ErrorMessage = "Mã khuyến mãi tối đa 32 ký tự.")]
    string? PromotionCode
);

/// <summary>
/// DTO chứa trạng thái mới khi cập nhật đơn hàng.
/// </summary>
public record StatusRequest(
    [Required(ErrorMessage = "Trạng thái đơn hàng không được để trống.")]
    [StringLength(30, ErrorMessage = "Trạng thái tối đa 30 ký tự.")]
    string Status
);

/// <summary>
/// DTO chứa nội dung tin nhắn chat trong đơn hàng.
/// </summary>
public record MessageRequest(
    [Required(ErrorMessage = "Nội dung tin nhắn không được để trống.")]
    [StringLength(2000, ErrorMessage = "Nội dung tin nhắn tối đa 2000 ký tự.")]
    string Content
);

/// <summary>
/// DTO chứa số tiền dùng cho các giao dịch ví (nạp tiền / rút tiền).
/// </summary>
public record AmountRequest(
    [Range(1000, 1000000000, ErrorMessage = "Số tiền giao dịch tối thiểu là 1,000 VND.")]
    decimal Amount
);

/// <summary>
/// DTO chứa tọa độ GPS và trạng thái sẵn sàng nhận đơn của tài xế.
/// </summary>
public record LocationRequest(
    [Range(-90, 90, ErrorMessage = "Vĩ độ GPS (Latitude) không hợp lệ.")]
    double Latitude,

    [Range(-180, 180, ErrorMessage = "Kinh độ GPS (Longitude) không hợp lệ.")]
    double Longitude,

    bool Online
);

/// <summary>
/// DTO chứa đánh giá (số sao và phản hồi) của khách hàng cho tài xế.
/// </summary>
public record ReviewRequest(
    [Range(1, 5, ErrorMessage = "Số sao đánh giá phải nằm trong khoảng từ 1 đến 5.")]
    int Stars,

    [StringLength(1000, ErrorMessage = "Phản hồi tối đa 1000 ký tự.")]
    string Feedback
);

/// <summary>
/// DTO chứa nội dung tin nhắn yêu cầu hỗ trợ (Support) từ người dùng.
/// </summary>
public record SupportRequest(
    [Required(ErrorMessage = "Nội dung hỗ trợ không được để trống.")]
    [StringLength(2000, ErrorMessage = "Nội dung hỗ trợ tối đa 2000 ký tự.")]
    string Content
);