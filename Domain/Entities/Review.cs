using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Quản lý đánh giá và phản hồi của khách hàng dành cho tài xế sau khi hoàn thành đơn hàng.
/// </summary>
public class Review
{
    public int Id { get; set; } // Mã đánh giá

    public int OrderId { get; set; } // ID đơn hàng được đánh giá

    public int CustomerId { get; set; } // ID khách hàng đánh giá

    public int DriverId { get; set; } // ID tài xế được đánh giá

    public int Stars { get; set; } // Số sao đánh giá (1 đến 5)

    [MaxLength(1000)]
    public string Feedback { get; set; } = ""; // Phản hồi / Nhận xét của khách hàng

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // Thời điểm đánh giá (UTC)
}