namespace Logistics.Api.Domain.DTOs.Responses
{
    /// <summary>
    /// Thông tin rút gọn của tài xế được đính kèm trong chi tiết đơn hàng để Client hiển thị.
    /// </summary>
    public record DriverBriefResponse(
        string Name,         // Tên tài xế
        string Phone,        // SĐT tài xế
        string? LicensePlate // Biển số xe của tài xế
    );

    /// <summary>
    /// Đại diện cho cấu trúc dữ liệu đơn hàng chi tiết (Order Response DTO) 
    /// trả về khi khách hàng hoặc tài xế truy vấn thông tin đơn.
    /// </summary>
    public record OrderResponse(
        int Id,                      // ID nội bộ của đơn hàng trong cơ sở dữ liệu
        string TrackingCode,         // Mã vận đơn hiển thị để tra cứu
        int CustomerId,              // ID khách hàng tạo đơn
        int? DriverId,               // ID tài xế nhận đơn (có thể null nếu chưa ai nhận)
        string? Customer,            // Tên khách hàng
        DriverBriefResponse? Driver, // Thông tin chi tiết rút gọn của tài xế
        string PickupAddress,        // Địa chỉ lấy hàng
        string DropoffAddress,       // Địa chỉ giao hàng
        double? PickupLat,           // Vĩ độ điểm lấy hàng
        double? PickupLng,           // Kinh độ điểm lấy hàng
        double? DropoffLat,          // Vĩ độ điểm giao hàng
        double? DropoffLng,          // Kinh độ điểm giao hàng
        string Status,               // Trạng thái hiện tại của đơn hàng (dạng chuỗi)
        string VehicleType,          // Loại phương tiện yêu cầu (MOTORBIKE, TRUCK,...)
        string ServiceType,          // Loại dịch vụ chi tiết (express, truck, container)
        string ServiceName,          // Tên dịch vụ hiển thị tiếng Việt thân thiện
        string SenderName,           // Tên người gửi
        string SenderPhone,          // SĐT người gửi
        string ReceiverName,         // Tên người nhận
        string ReceiverPhone,        // SĐT người nhận
        string PackageName,          // Tên mặt hàng
        double WeightKg,             // Trọng lượng hàng hóa (kg)
        string PackageDetails,       // Mô tả chi tiết hàng hóa
        string? Note,                // Ghi chú thêm cho tài xế
        double DistanceKm,           // Quãng đường giao hàng tính toán (km)
        decimal BasePrice,           // Giá cước cơ bản
        decimal ShippingFee,         // Phí vận chuyển thực tế
        decimal ExtraFee,            // Các khoản phụ phí phát sinh thêm
        decimal TotalPrice,          // Tổng số tiền khách phải thanh toán
        decimal DriverEarning,       // Thu nhập dự kiến của tài xế cho đơn này
        string PaymentMethod,        // Phương thức thanh toán (CASH, BANK_TRANSFER,...)
        bool IsBulky,                // Hàng cồng kềnh hay không
        bool IsDoorDelivery,         // Yêu cầu giao tận cửa/lên tầng hay không
        string? PromotionCode,       // Mã khuyến mãi đã áp dụng
        decimal CodAmount,           // Số tiền thu hộ COD
        DateTime CreatedAtUtc,       // Thời điểm tạo đơn (UTC)
        DateTime UpdatedAtUtc,       // Thời điểm cập nhật gần nhất (UTC)
        DateTime? CompletedAtUtc,    // Thời điểm hoàn thành đơn (UTC)
        string? ProofPhotoPath       // Đường dẫn ảnh xác thực giao hàng thành công (POD)
    );
}
