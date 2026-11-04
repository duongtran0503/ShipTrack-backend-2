using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.DTOs.Requests;

public class UpsertServiceAreaRequest
{
    [Required(ErrorMessage = "Tên khu vực không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên khu vực tối đa 100 ký tự.")]
    public string Name { get; set; } = "";

    [Range(-90, 90, ErrorMessage = "Vĩ độ tâm vùng (CenterLat) không hợp lệ.")]
    public double CenterLat { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ tâm vùng (CenterLng) không hợp lệ.")]
    public double CenterLng { get; set; }

    [Range(0.5, 100, ErrorMessage = "Bán kính phục vụ phải từ 0.5km đến 100km.")]
    public double RadiusKm { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}