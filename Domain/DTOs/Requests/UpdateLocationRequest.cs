using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.DTOs.Requests;

public class UpdateLocationRequest
{
    [Required(ErrorMessage = "Vĩ độ (Latitude) là bắt buộc.")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Kinh độ (Longitude) là bắt buộc.")]
    public double Longitude { get; set; }
}