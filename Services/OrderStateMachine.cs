namespace Logistics.Api.Services;

/// <summary>
/// Lớp quản lý quy tắc chuyển đổi trạng thái đơn hàng (Order State Machine), 
/// được sử dụng chung bởi tầng API và các bài kiểm thử (Unit Tests).
/// </summary>
public static class OrderStateMachine
{
    /// <summary>
    /// Kiểm tra xem việc chuyển đổi trạng thái đơn hàng có hợp lệ hay không 
    /// dựa trên trạng thái hiện tại, trạng thái tiếp theo, vai trò và ID của người thực hiện.
    /// </summary>
    public static bool CanTransition(string current, string next, string role, int actorId, int customerId, int? driverId) =>
        (current, next) switch
        {
            // Khách hàng có quyền hủy đơn khi đơn đang ở trạng thái chờ (pending)
            ("pending", "cancelled") => role == "customer" && actorId == customerId,

            // Tài xế xác nhận đã đến điểm lấy hàng
            ("accepted", "arrived_pickup") => role.StartsWith("driver") && actorId == driverId,

            // Tài xế xác nhận đã nhận hàng và bắt đầu di chuyển giao hàng
            ("arrived_pickup", "delivering") => role.StartsWith("driver") && actorId == driverId,

            // Tài xế xác nhận đã giao hàng thành công
            ("delivering", "completed") => role.StartsWith("driver") && actorId == driverId,

            // Các luồng chuyển trạng thái khác đều không hợp lệ
            _ => false
        };
}