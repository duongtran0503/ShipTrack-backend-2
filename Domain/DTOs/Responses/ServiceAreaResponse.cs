namespace Logistics.Api.Domain.DTOs.Responses;

public class ServiceAreaResponse
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public string Name { get; set; } = "";
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public double RadiusKm { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public static class ServiceAreaMappingExtensions
{
    public static ServiceAreaResponse ToResponse(this Logistics.Api.Domain.Entities.DriverServiceArea a) => new()
    {
        Id = a.Id,
        DriverId = a.DriverId,
        Name = a.Name,
        CenterLat = a.CenterLat,
        CenterLng = a.CenterLng,
        RadiusKm = a.RadiusKm,
        IsDefault = a.IsDefault,
        IsActive = a.IsActive,
        CreatedAtUtc = a.CreatedAtUtc
    };
}