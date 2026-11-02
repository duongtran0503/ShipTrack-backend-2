namespace Logistics.Api.Domain.Constants;

public enum OrderStatus
{
    PENDING,          // Đang chờ xử lý
    SEARCHING_DRIVER, // Đang tìm tài xế
    ACCEPTED,         // Tài xế đã nhận đơn
    ARRIVED_PICKUP,   // Tài xế đã đến điểm lấy hàng
    DELIVERING,       // Đang giao hàng
    COMPLETED,        // Đã hoàn thành
    CANCELLED         // Đã hủy
}