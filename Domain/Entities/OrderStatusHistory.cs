using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Lưu trữ lịch sử thay đổi trạng thái của đơn hàng theo thời gian.
/// </summary>
public class OrderStatusHistory
{
    public int Id { get; set; } // Mã dòng lịch sử

    public int OrderId { get; set; } // ID đơn hàng liên quan

    public int ActorId { get; set; } // ID người thực hiện thay đổi trạng thái (User / Admin / System)

    [MaxLength(25)]
    public string Status { get; set; } = ""; // Trạng thái đơn tại thời điểm ghi nhận

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow; // Thời điểm chuyển trạng thái (UTC)
}