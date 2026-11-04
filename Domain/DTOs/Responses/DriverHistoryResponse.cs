namespace Logistics.Api.Domain.DTOs.Responses;

public class DriverHistoryListResponse
{
    public List<HistoryOrderItemDto> Items { get; set; } = [];
    public PaginationDto Pagination { get; set; } = new();
}

public class HistoryOrderItemDto
{
    public string Id { get; set; } = "";
    public string TrackingCode { get; set; } = "";
    public string Status { get; set; } = "";
    public string SenderAddress { get; set; } = "";
    public string? PickupAddress { get; set; }
    public double? PickupLat { get; set; }
    public double? PickupLng { get; set; }
    public string ReceiverAddress { get; set; } = "";
    public double? DropoffLat { get; set; }
    public double? DropoffLng { get; set; }
    public string ReceiverName { get; set; } = "";
    public string ReceiverPhone { get; set; } = "";
    public string PackageName { get; set; } = "";
    public double WeightKg { get; set; }
    public decimal ShippingFee { get; set; }
    public string VehicleType { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public decimal CodAmount { get; set; }
    public double DistanceKm { get; set; }
    public string CreatedAt { get; set; } = "";
    public string? CompletedAt { get; set; }
    public double? ReviewRating { get; set; }
    public string? ReviewComment { get; set; }
    public string? FailureReason { get; set; }
}

public class DriverHistoryKpiResponse { 
    public int TotalFinished { get; set; }
    public string CompletionRate { get; set; } = "0";
    public string FailureRate { get; set; } = "0";
    public string AvgRating { get; set; } = "5.0";
    public int AvgDeliveryMinutes { get; set; }
    public string TotalDistance { get; set; } = "0";
}

public class DriverHistoryChartResponse
{
    public string Period { get; set; } = "";
    public List<ChartItemDto> ChartData { get; set; } = [];
}

public class OrderTimelineItemDto
{
    public string Status { get; set; } = "";
    public string At { get; set; } = "";
}

public class DriverOrderDetailResponse : HistoryOrderItemDto
{
    public bool IsBulky { get; set; }
    public bool IsDoorDelivery { get; set; }
    public string? AcceptedAt { get; set; }
    public string? PickedUpAt { get; set; }
    public string? FailedAt { get; set; }
    public List<OrderTimelineItemDto> Timeline { get; set; } = [];
}