using Logistics.Api.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Đại diện cho bảng đơn hàng vận chuyển trong hệ thống, đồng bộ với Frontend và sử dụng Enum quản lý giá trị giới hạn.
/// </summary>
public class DeliveryOrder
{
    public int Id { get; set; } // Mã định danh đơn hàng (Database ID)

    [MaxLength(50)]
    public string TrackingCode { get; set; } = ""; // Mã vận đơn hiển thị cho người dùng

    public int CustomerId { get; set; } // ID người đặt hàng

    public User Customer { get; set; } = null!; // Thông tin người đặt hàng

    public int? DriverId { get; set; } // ID tài xế nhận đơn

    public User? Driver { get; set; } // Thông tin tài xế nhận đơn

    // Sử dụng Enum cho trạng thái đơn hàng
    public OrderStatus Status { get; set; } = OrderStatus.SEARCHING_DRIVER;

    // Sử dụng Enum cho loại phương tiện
    public VehicleType VehicleType { get; set; } = VehicleType.MOTORBIKE;

    [MaxLength(25)]
    public string ServiceType { get; set; } = "express"; // Loại dịch vụ chi tiết

    [MaxLength(500)]
    public string PickupAddress { get; set; } = ""; // Địa chỉ lấy hàng

    [MaxLength(500)]
    public string DropoffAddress { get; set; } = ""; // Địa chỉ giao hàng

    public double? PickupLat { get; set; } // Vĩ độ điểm lấy hàng
    public double? PickupLng { get; set; } // Kinh độ điểm lấy hàng
    public double? DropoffLat { get; set; } // Vĩ độ điểm giao hàng
    public double? DropoffLng { get; set; } // Kinh độ điểm giao hàng
    public double DistanceKm { get; set; } // Khoảng cách giao hàng (km)

    [MaxLength(120)]
    public string SenderName { get; set; } = ""; // Tên người gửi

    [MaxLength(30)]
    public string SenderPhone { get; set; } = ""; // SĐT người gửi

    [MaxLength(120)]
    public string ReceiverName { get; set; } = ""; // Tên người nhận

    [MaxLength(30)]
    public string ReceiverPhone { get; set; } = ""; // SĐT người nhận

    [MaxLength(150)]
    public string PackageName { get; set; } = ""; // Tên hàng hóa

    public double WeightKg { get; set; } // Trọng lượng hàng hóa (kg)

    [MaxLength(1000)]
    public string PackageDetails { get; set; } = ""; // Mô tả chi tiết hàng hóa

    [MaxLength(500)]
    public string? Note { get; set; } // Ghi chú thêm

    // Sử dụng Enum cho phương thức thanh toán
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;

    public bool IsBulky { get; set; } // Hàng cồng kềnh (phụ phí)
    public bool IsDoorDelivery { get; set; } // Giao hàng tận cửa/lên tầng
    public decimal TipAmount { get; set; } // Tiền tip cho tài xế
    public decimal CodAmount { get; set; } // Tiền thu hộ (COD)
    public decimal BasePrice { get; set; } // Giá cước gốc
    public decimal ShippingFee { get; set; } = 0; // Phí vận chuyển thực tế
    public decimal TotalPrice { get; set; } // Tổng tiền khách phải trả

    [MaxLength(32)]
    public string? PromotionCode { get; set; } // Mã khuyến mãi
    public decimal DiscountAmount { get; set; } // Số tiền giảm giá

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // Thời điểm tạo đơn
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow; // Thời điểm cập nhật

    [MaxLength(250)]
    public string? ProofPhotoPath { get; set; } // Ảnh xác thực giao hàng (POD)
    public bool PaidOut { get; set; } // Đã quyết toán tiền cho tài xế chưa

    public List<OrderStatusHistory> History { get; set; } = []; // Lịch sử trạng thái
    // 🔥 Bổ sung thêm các mốc thời gian còn thiếu
    public DateTime? AcceptedAtUtc { get; set; }   // Thời điểm tài xế nhận đơn
    public DateTime? PickedUpAtUtc { get; set; }  // Thời điểm tài xế lấy hàng (PICKED_UP)
    public DateTime? FailedAtUtc { get; set; }    // Thời điểm đơn hàng bị hủy/thất bại
    public DateTime? CompletedAtUtc { get; set; } // Thời điểm hoàn thành
    public String FailureReason { get; set; } = "";
}