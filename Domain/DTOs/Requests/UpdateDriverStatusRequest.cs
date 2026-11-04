using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.Domain.DTOs.Requests;

public class UpdateDriverStatusRequest
{
    [Required(ErrorMessage = "Trạng thái hoạt động là bắt buộc.")]
    public bool IsOnline { get; set; }
}