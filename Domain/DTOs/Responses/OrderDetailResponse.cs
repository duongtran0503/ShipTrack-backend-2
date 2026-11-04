namespace Logistics.Api.Domain.DTOs.Responses;

public class OrderDetailResponse
{
    public int Id { get; set; }
    public string TrackingCode { get; set; } = "";
    public string PickupAddress { get; set; } = "";
    public string DropoffAddress { get; set; } = "";
    public double? PickupLat { get; set; }
    public double? PickupLng { get; set; }
    public double? DropoffLat { get; set; }
    public double? DropoffLng { get; set; }
    public double DistanceKm { get; set; }
    public string SenderName { get; set; } = "";
    public string SenderPhone { get; set; } = "";
    public string ReceiverName { get; set; } = "";
    public string ReceiverPhone { get; set; } = "";
    public string PackageName { get; set; } = "";
    public double WeightKg { get; set; }
    public string PackageDetails { get; set; } = "";
    public string? Note { get; set; }
    public string PaymentMethod { get; set; } = "";
    public bool IsBulky { get; set; }
    public bool IsDoorDelivery { get; set; }
    public decimal TipAmount { get; set; }
    public decimal CodAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalPrice { get; set; }
    public string ServiceType { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}