using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Quản lý tin nhắn trao đổi trực tuyến giữa khách hàng và tài xế trong một đơn hàng.
/// </summary>
public class OrderMessage
{
    public int Id { get; set; } // Mã tin nhắn

    public int OrderId { get; set; } // ID đơn hàng đang trao đổi

    public int SenderId { get; set; } // ID người gửi tin nhắn (Khách hàng hoặc Tài xế)

    [MaxLength(2000)]
    public string Content { get; set; } = ""; // Nội dung tin nhắn chat

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // Thời điểm gửi tin nhắn (UTC)
}