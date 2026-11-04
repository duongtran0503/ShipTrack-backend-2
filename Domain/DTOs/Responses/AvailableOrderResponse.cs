namespace Logistics.Api.Domain.DTOs.Responses
{
    public class AvailableOrderResponse
    {
        public int Id { get; set; }
        public string TrackingCode { get; set; } = "";
        public string PickupAddress { get; set; } = "";
        public string DropoffAddress { get; set; } = "";
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public double DropoffLat { get; set; }
        public double DropoffLng { get; set; }

        /// <summary>Khoảng cách từ driver tới điểm lấy hàng (km).</summary>
        public double DistanceFromDriverKm { get; set; }

        /// <summary>Độ dài chuyến đi (pickup → dropoff) (km).</summary>
        public double TripDistanceKm { get; set; }

        public decimal ShippingFee { get; set; }
        public string ServiceType { get; set; } = "";
        public string VehicleType { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
    }
}
