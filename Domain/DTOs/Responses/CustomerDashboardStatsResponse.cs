namespace Logistics.Api.Domain.DTOs.Responses;

/// <summary>
/// DTO thống kê nhanh cho Customer Dashboard.
/// </summary>
public class CustomerDashboardStatsResponse
{
    /// <summary>Số đơn đang hoạt động (chưa hoàn thành / chưa hủy).</summary>
    public int ActiveOrders { get; set; }

    /// <summary>Số đơn đã hoàn thành.</summary>
    public int CompletedOrders { get; set; }

    /// <summary>Tổng số đơn hàng (mọi trạng thái).</summary>
    public int TotalOrders { get; set; }

    /// <summary>Tổng chi phí vận chuyển của các đơn đã hoàn thành (VND).</summary>
    public decimal TotalSpent { get; set; }
}