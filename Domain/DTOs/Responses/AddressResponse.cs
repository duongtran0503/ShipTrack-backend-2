namespace Logistics.Api.Domain.DTOs.Responses;

public record AddressResponse(
    int Id,
    int OwnerId,
    string Label,
    string ContactName,
    string ContactPhone,
    string FullAddress,
    bool IsDefault,
    double Lat,
    double Lng,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public static class AddressMappingExtensions
{
    public static AddressResponse ToResponse(this Logistics.Api.Domain.Entities.Address a) => new(
        Id: a.Id,
        OwnerId: a.OwnerId,
        Label: a.Label,
        ContactName: a.ContactName,
        ContactPhone: a.ContactPhone,
        FullAddress: a.FullAddress,
        IsDefault: a.IsDefault,
        Lat: a.Lat,
        Lng: a.Lng,
        CreatedAtUtc: a.CreatedAtUtc,
        UpdatedAtUtc: a.UpdatedAtUtc
    );
}