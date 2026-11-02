using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Entities;

namespace Logistics.Api.Domain.DTOs.Mappings
{
    /// <summary>
    /// Lớp tiện ích mở rộng (Extension Methods) chuyên trách việc ánh xạ (Mapping) 
    /// từ các Entity Database sang các Response DTOs trả về cho Client.
    /// </summary>
    public static class OrderMappingExtensions
    {
        /// <summary>
        /// Chuyển đổi thực thể User sang UserResponse DTO.
        /// </summary>
        public static UserResponse ToResponse(this User u) => new(
            Id: u.Id,
            UserId: u.Id,
            Name: u.Name,
            Email: u.Email,
            Role: u.Role,
            Phone: u.Phone,
            LicensePlate: u.LicensePlate,
            Balance: u.Balance,
            IsActive: u.IsActive,
            BanReason: u.BanReason,
            Online: u.Online
        );

        /// <summary>
        /// Chuyển đổi thực thể DeliveryOrder sang OrderResponse DTO chi tiết.
        /// </summary>
        public static OrderResponse ToResponse(this DeliveryOrder o) => new(
            Id: o.Id,
            TrackingCode: o.TrackingCode,
            CustomerId: o.CustomerId,
            DriverId: o.DriverId,
            Customer: o.Customer?.Name,
            Driver: o.Driver == null ? null : new DriverBriefResponse(o.Driver.Name, o.Driver.Phone, o.Driver.LicensePlate),
            PickupAddress: o.PickupAddress,
            DropoffAddress: o.DropoffAddress,
            PickupLat: o.PickupLat,
            PickupLng: o.PickupLng,
            DropoffLat: o.DropoffLat,
            DropoffLng: o.DropoffLng,
            Status: o.Status.ToString(),
            VehicleType: o.VehicleType.ToString(),
            ServiceType: o.ServiceType,
            ServiceName: o.ServiceType switch
            {
                "truck" => "Xe tải 1.5 tấn",
                "container" => "Xe container",
                _ => "Xe máy giao nhanh"
            },
            SenderName: o.SenderName,
            SenderPhone: o.SenderPhone,
            ReceiverName: o.ReceiverName,
            ReceiverPhone: o.ReceiverPhone,
            PackageName: o.PackageName,
            WeightKg: o.WeightKg,
            PackageDetails: o.PackageDetails,
            Note: o.Note,
            DistanceKm: o.DistanceKm,
            BasePrice: o.BasePrice,
            ShippingFee: o.ShippingFee,
            ExtraFee: o.TotalPrice - o.BasePrice - o.TipAmount + o.DiscountAmount,
            TotalPrice: o.TotalPrice,
            DriverEarning: Math.Round(o.TotalPrice * 0.8m, 0),
            PaymentMethod: o.PaymentMethod.ToString(),
            IsBulky: o.IsBulky,
            IsDoorDelivery: o.IsDoorDelivery,
            PromotionCode: o.PromotionCode,
            CodAmount: o.CodAmount,
            CreatedAtUtc: o.CreatedAtUtc,
            UpdatedAtUtc: o.UpdatedAtUtc,
            CompletedAtUtc: o.CompletedAtUtc,
            ProofPhotoPath: o.ProofPhotoPath
        );
    }
}
