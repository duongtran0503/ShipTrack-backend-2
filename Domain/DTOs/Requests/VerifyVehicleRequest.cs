using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.DTOs.Requests;

public class VerifyVehicleRequest
{
    [Required(ErrorMessage = "Trạng thái duyệt (isVerified) là bắt buộc.")]
    public bool IsVerified { get; set; }
}