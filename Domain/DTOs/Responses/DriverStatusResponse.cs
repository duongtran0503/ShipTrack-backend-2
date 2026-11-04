using Logistics.Api.Domain.Entities;

namespace Logistics.Api.Domain.DTOs.Responses;

public class DriverStatusResponse
{
    public int DriverId { get; set; }
    public string DriverName { get; set; } = "";
    public bool IsOnline { get; set; }
    public DateTime ChangedAtUtc { get; set; }

    public static DriverStatusResponse FromEntity(User u) => new()
    {
        DriverId = u.Id,
        DriverName = u.Name,
        IsOnline = u.Online,
        ChangedAtUtc = DateTime.UtcNow,
    };
}