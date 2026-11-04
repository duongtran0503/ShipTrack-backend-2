namespace Logistics.Api.Domain.Constants;

public enum OrderStatus
{
    PENDING,          // Đang chờ xử lý
    SEARCHING_DRIVER, // Đang tìm tài xế

    DRIVER_ASSIGNED,
    PICKED_UP,
    DELIVERING,       // Đang giao hàng
    COMPLETED,        // Đã hoàn thành
    CANCELLED,         // Đã hủy
        FAILED,
}