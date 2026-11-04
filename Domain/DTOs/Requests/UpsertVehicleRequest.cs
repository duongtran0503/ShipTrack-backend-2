using System.ComponentModel.DataAnnotations;
using Logistics.Api.Domain.Constants;

namespace Logistics.Api.Domain.DTOs.Requests;

public class UpsertVehicleRequest
{
    [Required(ErrorMessage = "Loại phương tiện là bắt buộc.")]
    public VehicleType VehicleType { get; set; }

    [Required(ErrorMessage = "Biển số xe không được để trống.")]
    [StringLength(30, ErrorMessage = "Biển số xe tối đa 30 ký tự.")]
    public string LicensePlate { get; set; } = "";

    [StringLength(50, ErrorMessage = "Hãng xe tối đa 50 ký tự.")]
    public string? Brand { get; set; }

    [StringLength(30, ErrorMessage = "Màu xe tối đa 30 ký tự.")]
    public string? Color { get; set; }

    [Range(1, 100000, ErrorMessage = "Tải trọng tối đa không hợp lệ.")]
    public double? MaxWeightKg { get; set; }
}