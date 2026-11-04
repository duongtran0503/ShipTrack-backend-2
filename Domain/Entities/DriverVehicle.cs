using Logistics.Api.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.Entities;

/// <summary>
/// Phương tiện của tài xế. Mỗi tài xế có thể có nhiều phương tiện
/// (xe máy, xe tải nhỏ, xe container...).
/// </summary>
public class DriverVehicle
{
    public int Id { get; set; }

    public int DriverId { get; set; }

    /// <summary>Loại phương tiện: MOTORBIKE, TRUCK, CONTAINER...</summary>
    [MaxLength(409)]
    public VehicleType VehicleType { get; set; } 

    /// <summary>Biển số xe.</summary>
    [MaxLength(30)]
    public string LicensePlate { get; set; } = "";

    /// <summary>Hãng xe: Honda, Toyota, Isuzu...</summary>
    [MaxLength(50)]
    public string? Brand { get; set; }

    /// <summary>Màu xe.</summary>
    [MaxLength(30)]
    public string? Color { get; set; }

    /// <summary>Tải trọng tối đa (kg).</summary>
    public double? MaxWeightKg { get; set; }

    /// <summary>Phương tiện mặc định (mỗi driver chỉ 1).</summary>
    public bool IsDefault { get; set; }

    /// <summary>Đã được admin duyệt chưa.</summary>
    public bool IsVerified { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Driver { get; set; }
}