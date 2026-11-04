using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.DTOs.Requests;

public class UpsertAddressRequest
{
    [Required(ErrorMessage = "Nhãn địa chỉ không được để trống.")]
    [StringLength(50, ErrorMessage = "Nhãn tối đa 50 ký tự.")]
    public string Label { get; set; } = "";

    [Required(ErrorMessage = "Tên người liên hệ không được để trống.")]
    [StringLength(120, ErrorMessage = "Tên liên hệ tối đa 120 ký tự.")]
    public string ContactName { get; set; } = "";

    [Required(ErrorMessage = "Số điện thoại liên hệ không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
    public string ContactPhone { get; set; } = "";

    [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống.")]
    [StringLength(500, ErrorMessage = "Địa chỉ chi tiết tối đa 500 ký tự.")]
    public string FullAddress { get; set; } = "";

    public bool IsDefault { get; set; }

    [Range(-90, 90, ErrorMessage = "Vĩ độ (Lat) không hợp lệ.")]
    public double Lat { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ (Lng) không hợp lệ.")]
    public double Lng { get; set; }
}