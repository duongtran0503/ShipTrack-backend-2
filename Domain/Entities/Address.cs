using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Địa chỉ trong "danh bạ địa chỉ" của một User.
/// Mỗi User có thể lưu nhiều địa chỉ của nhiều người khác nhau
/// (người thân, đồng nghiệp, kho hàng, đối tác...) để tái sử dụng khi tạo đơn.
/// </summary>
public class Address
{
    public int Id { get; set; }

    /// <summary>
    /// FK tới User sở hữu địa chỉ này (chủ danh bạ).
    /// Đây KHÔNG phải người liên hệ tại địa chỉ — đó là ContactName/ContactPhone.
    /// </summary>
    public int OwnerId { get; set; }

    /// <summary>Nhãn địa chỉ: "Nhà mẹ", "Công ty", "Kho Q7"...</summary>
    [MaxLength(50)]
    public string Label { get; set; } = "";

    /// <summary>Tên người liên hệ TẠI địa chỉ này (không phải chủ danh bạ).</summary>
    [MaxLength(120)]
    public string ContactName { get; set; } = "";

    /// <summary>SĐT người liên hệ tại địa chỉ này.</summary>
    [MaxLength(15)]
    public string ContactPhone { get; set; } = "";

    /// <summary>Địa chỉ chi tiết đầy đủ.</summary>
    [MaxLength(500)]
    public string FullAddress { get; set; } = "";

    /// <summary>Địa chỉ mặc định của danh bạ (mỗi Owner tối đa 1).</summary>
    public bool IsDefault { get; set; }

    /// <summary>Vĩ độ GPS.</summary>
    public double Lat { get; set; }

    /// <summary>Kinh độ GPS.</summary>
    public double Lng { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Owner { get; set; }
}