using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Quản lý hộp thư tin nhắn hỗ trợ giữa người dùng và tổng đài/bot.
/// </summary>
public class SupportMessage
{
    public int Id { get; set; } // Mã tin nhắn hỗ trợ

    public int UserId { get; set; } // ID người dùng cần hỗ trợ

    [MaxLength(20)]
    public string SenderType { get; set; } = "user"; // Người gửi: user, bot, admin

    [MaxLength(2000)]
    public string Content { get; set; } = ""; // Nội dung tin nhắn hỗ trợ

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // Thời điểm gửi tin nhắn (UTC)
}