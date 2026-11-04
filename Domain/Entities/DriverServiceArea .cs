using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Khu vực phục vụ của tài xế. Mỗi driver có thể có nhiều vùng
/// (VD: bán kính 10km quanh Quận 1, bán kính 15km quanh Thủ Đức).
/// Đơn khả dụng = đơn mà CẢ pickup và dropoff đều nằm trong 1 vùng.
/// </summary>
public class DriverServiceArea
{
    public int Id { get; set; }

    public int DriverId { get; set; }

    /// <summary>Tên vùng: "Quận 1", "Thủ Đức", "Toàn TP.HCM"...</summary>
    [MaxLength(100)]
    public string Name { get; set; } = "";

    /// <summary>Tâm vùng — vĩ độ.</summary>
    public double CenterLat { get; set; }

    /// <summary>Tâm vùng — kinh độ.</summary>
    public double CenterLng { get; set; }

    /// <summary>Bán kính phục vụ (km).</summary>
    public double RadiusKm { get; set; }

    /// <summary>Vùng mặc định (mỗi driver chỉ 1).</summary>
    public bool IsDefault { get; set; }

    /// <summary>Vùng đang hoạt động.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Driver { get; set; }
}