using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.Entities;

namespace Logistics.Api.Domain.DTOs.Responses;

public class DriverVehicleResponse
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public string VehicleType { get; set; } = "";
    public string LicensePlate { get; set; } = "";
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public double? MaxWeightKg { get; set; }
    public bool IsDefault { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public static class DriverVehicleMappingExtensions
{
    public static DriverVehicleResponse ToResponse(this DriverVehicle v) => new()
    {
        Id = v.Id,
        DriverId = v.DriverId,
        VehicleType = v.VehicleType.ToString(),
        LicensePlate = v.LicensePlate,
        Brand = v.Brand,
        Color = v.Color,
        MaxWeightKg = v.MaxWeightKg,
        IsDefault = v.IsDefault,
        IsVerified = v.IsVerified,
        CreatedAtUtc = v.CreatedAtUtc
    };
}