namespace Logistics.Api.Domain.DTOs.Responses;

public class EarningsChartResponse
{
    public string Period { get; set; } = "";
    public List<EarningsChartItemDto> ChartData { get; set; } = [];
}

public class EarningsChartItemDto
{
    public string Label { get; set; } = "";
    public decimal Income { get; set; }
}