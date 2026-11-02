using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Quản lý sổ cái biến động số dư ví tiền của người dùng (nạp tiền, rút tiền, nhận hoa hồng, trừ phí).
/// </summary>
public class WalletTransaction
{
    public int Id { get; set; } // Mã giao dịch ví

    public int UserId { get; set; } // ID người dùng sở hữu giao dịch

    public int? OrderId { get; set; } // ID đơn hàng liên quan (nếu giao dịch từ đơn hàng)

    [MaxLength(30)]
    public string Type { get; set; } = ""; // Loại giao dịch: topup, withdrawal, income, deduction

    public decimal Amount { get; set; } // Số tiền biến động (dương: cộng tiền, âm: trừ tiền)

    [MaxLength(300)]
    public string Description { get; set; } = ""; // Mô tả chi tiết giao dịch

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // Thời điểm thực hiện giao dịch (UTC)
}