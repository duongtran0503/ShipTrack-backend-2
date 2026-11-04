namespace Logistics.Api.Domain.DTOs.Responses;

public class DriverEarningsResponse
{
    public EarningsSummaryDto Summary { get; set; } = new();
    public List<EarningItemDto> EarningsList { get; set; } = [];
    public PaginationDto Pagination { get; set; } = new();
}

public class EarningsSummaryDto
{
    public decimal Today { get; set; }
    public decimal ThisWeek { get; set; }
    public decimal ThisMonth { get; set; }
    public decimal PendingWithdrawal { get; set; }
}

public class EarningItemDto
{
    public string Id { get; set; } = "";
    public string TrackingCode { get; set; } = "";
    public string CompletedAt { get; set; } = ""; // ISO 8601 UTC+7
    public decimal ShippingFee { get; set; }
    public decimal DriverBonus { get; set; }
    public decimal PlatformCommission { get; set; }
    public decimal NetIncome { get; set; }
    public string PaymentMethod { get; set; } = "";
}

public class PaginationDto
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
}

public class DriverStatisticsResponse
{
    public int TotalTrips { get; set; }
    public string CompletionRate { get; set; } = "0";
    public string FailureRate { get; set; } = "0";
    public string AvgRating { get; set; } = "0";
    public int AvgDeliveryMinutes { get; set; }
    public string TotalDistanceKm { get; set; } = "0";
    public DriverRankingDto Ranking { get; set; } = new();
    public List<ChartItemDto> ChartData { get; set; } = [];
}

public class DriverRankingDto
{
    public int Position { get; set; }
    public int TotalDrivers { get; set; }
    public string BadgeName { get; set; } = "";
}

public class ChartItemDto
{
    public string Label { get; set; } = "";
    public int Completed { get; set; }
    public int Failed { get; set; }
}