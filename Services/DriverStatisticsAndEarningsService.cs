using Logistics.Api.Data;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Responses;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services;

public class DriverStatisticsAndEarningsService
{
    private readonly AppDbContext _db;
    private static readonly TimeZoneInfo VietNamZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public DriverStatisticsAndEarningsService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy báo cáo thu nhập và danh sách giao dịch của tài xế.
    /// </summary>
    public async Task<DriverEarningsResponse> GetDriverEarningsAsync(int driverId, string range, int page, int size, string? paymentMethod)
    {
        var nowUtc = DateTime.UtcNow;
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, VietNamZone);

        // 1. Tính toán mốc thời gian theo giờ VN
        var todayStartVn = nowVn.Date;
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStartVn, VietNamZone);

        int diff = (int)nowVn.DayOfWeek - 1;
        if (diff < 0) diff = 6;
        var weekStartVn = nowVn.Date.AddDays(-diff);
        var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(weekStartVn, VietNamZone);

        var monthStartVn = new DateTime(nowVn.Year, nowVn.Month, 1);
        var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthStartVn, VietNamZone);

        var baseQuery = _db.Orders.AsNoTracking()
            .Where(o => o.DriverId == driverId && o.Status == OrderStatus.COMPLETED);

        // Lấy danh sách đơn hoàn thành để tính tổng summary chính xác theo công thức mới
        var completedOrders = await baseQuery
            .Select(o => new { o.CompletedAtUtc, o.ShippingFee, o.TipAmount })
            .ToListAsync();

        // Công thức chuẩn: NetIncome = ShippingFee + TipAmount - PlatformCommission (2% phí ship)
        decimal CalculateNet(decimal shippingFee, decimal tip)
        {
            decimal commission = shippingFee * 0.02m; // Chiết khấu sàn 2%
            return shippingFee + tip - commission;
        }

        decimal todayTotal = completedOrders
            .Where(o => o.CompletedAtUtc >= todayStartUtc)
            .Sum(o => CalculateNet(o.ShippingFee, o.TipAmount));

        decimal weekTotal = completedOrders
            .Where(o => o.CompletedAtUtc >= weekStartUtc)
            .Sum(o => CalculateNet(o.ShippingFee, o.TipAmount));

        decimal monthTotal = completedOrders
            .Where(o => o.CompletedAtUtc >= monthStartUtc)
            .Sum(o => CalculateNet(o.ShippingFee, o.TipAmount));

        // 2. Lọc danh sách earnings theo range để phân trang hiển thị bảng
        var listQuery = baseQuery.AsQueryable();
        if (range.Equals("TODAY", StringComparison.OrdinalIgnoreCase))
        {
            listQuery = listQuery.Where(o => o.CompletedAtUtc >= todayStartUtc);
        }
        else if (range.Equals("THIS_WEEK", StringComparison.OrdinalIgnoreCase))
        {
            listQuery = listQuery.Where(o => o.CompletedAtUtc >= weekStartUtc);
        }
        else if (range.Equals("THIS_MONTH", StringComparison.OrdinalIgnoreCase))
        {
            listQuery = listQuery.Where(o => o.CompletedAtUtc >= monthStartUtc);
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            if (Enum.TryParse<PaymentMethod>(paymentMethod, true, out var pm))
            {
                listQuery = listQuery.Where(o => o.PaymentMethod == pm);
            }
        }

        var totalElements = await listQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalElements / (double)size);

        var pagedOrders = await listQuery
            .OrderByDescending(o => o.CompletedAtUtc)
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        var earningsList = pagedOrders.Select(o =>
        {
            var completedAtVn = o.CompletedAtUtc.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(o.CompletedAtUtc.Value, VietNamZone)
                : DateTime.Now;

            decimal shippingFee = o.ShippingFee;
            decimal driverBonus = o.TipAmount;
            decimal platformCommission = shippingFee * 0.02m; // Giả lập chiết khấu sàn 2%
            decimal netIncome = shippingFee + driverBonus - platformCommission;

            return new EarningItemDto
            {
                Id = $"earn_{o.Id}",
                TrackingCode = o.TrackingCode,
                CompletedAt = completedAtVn.ToString("yyyy-MM-ddTHH:mm:ss+07:00"),
                ShippingFee = shippingFee,
                DriverBonus = driverBonus,
                PlatformCommission = platformCommission,
                NetIncome = netIncome,
                PaymentMethod = o.PaymentMethod.ToString()
            };
        }).ToList();

        return new DriverEarningsResponse
        {
            Summary = new EarningsSummaryDto
            {
                Today = todayTotal,
                ThisWeek = weekTotal,
                ThisMonth = monthTotal,
                PendingWithdrawal = monthTotal
            },
            EarningsList = earningsList,
            Pagination = new PaginationDto
            {
                Page = page,
                Size = size,
                TotalElements = totalElements,
                TotalPages = totalPages == 0 ? 1 : totalPages
            }
        };
    }

    /// <summary>
    /// Lấy thông tin thống kê cá nhân (WEEK hoặc MONTH) kèm biểu đồ và bảng xếp hạng.
    /// </summary>
    public async Task<DriverStatisticsResponse> GetDriverStatisticsAsync(int driverId, string period)
    {
        var nowUtc = DateTime.UtcNow;
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, VietNamZone);

        DateTime periodStartUtc;
        List<ChartItemDto> chartData = [];

        if (period.Equals("WEEK", StringComparison.OrdinalIgnoreCase))
        {
            int diff = (int)nowVn.DayOfWeek - 1;
            if (diff < 0) diff = 6;
            var startVn = nowVn.Date.AddDays(-diff);
            periodStartUtc = TimeZoneInfo.ConvertTimeToUtc(startVn, VietNamZone);

            string[] labels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
            for (int i = 0; i < 7; i++)
            {
                var dayStartVn = startVn.AddDays(i);
                var dayEndVn = dayStartVn.AddDays(1);
                var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartVn, VietNamZone);
                var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(dayEndVn, VietNamZone);

                var completedCount = await _db.Orders.CountAsync(o => o.DriverId == driverId && o.Status == OrderStatus.COMPLETED && o.CreatedAtUtc >= dayStartUtc && o.CreatedAtUtc < dayEndUtc);
                var failedCount = await _db.Orders.CountAsync(o => o.DriverId == driverId && (o.Status == OrderStatus.FAILED || o.Status == OrderStatus.CANCELLED) && o.CreatedAtUtc >= dayStartUtc && o.CreatedAtUtc < dayEndUtc);

                chartData.Add(new ChartItemDto { Label = labels[i], Completed = completedCount, Failed = failedCount });
            }
        }
        else // MONTH
        {
            var startVn = new DateTime(nowVn.Year, nowVn.Month, 1);
            periodStartUtc = TimeZoneInfo.ConvertTimeToUtc(startVn, VietNamZone);

            for (int week = 1; week <= 4; week++)
            {
                var wStartVn = startVn.AddDays((week - 1) * 7);
                var wEndVn = week == 4 ? startVn.AddMonths(1) : wStartVn.AddDays(7);
                var wStartUtc = TimeZoneInfo.ConvertTimeToUtc(wStartVn, VietNamZone);
                var wEndUtc = TimeZoneInfo.ConvertTimeToUtc(wEndVn, VietNamZone);

                var completedCount = await _db.Orders.CountAsync(o => o.DriverId == driverId && o.Status == OrderStatus.COMPLETED && o.CreatedAtUtc >= wStartUtc && o.CreatedAtUtc < wEndUtc);
                var failedCount = await _db.Orders.CountAsync(o => o.DriverId == driverId && (o.Status == OrderStatus.FAILED || o.Status == OrderStatus.CANCELLED) && o.CreatedAtUtc >= wStartUtc && o.CreatedAtUtc < wEndUtc);

                chartData.Add(new ChartItemDto { Label = $"Tuần {week}", Completed = completedCount, Failed = failedCount });
            }
        }

        var periodOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.DriverId == driverId && o.CreatedAtUtc >= periodStartUtc)
            .ToListAsync();

        int totalTrips = periodOrders.Count;
        int completedTrips = periodOrders.Count(o => o.Status == OrderStatus.COMPLETED);
        int failedTrips = periodOrders.Count(o => o.Status == OrderStatus.FAILED || o.Status == OrderStatus.CANCELLED);

        double completionRate = totalTrips > 0 ? Math.Round((double)completedTrips / totalTrips * 100, 1) : 0;
        double failureRate = totalTrips > 0 ? Math.Round((double)failedTrips / totalTrips * 100, 1) : 0;
        double totalDistance = periodOrders.Sum(o => o.DistanceKm);

        return new DriverStatisticsResponse
        {
            TotalTrips = totalTrips,
            CompletionRate = completionRate.ToString("0.0"),
            FailureRate = failureRate.ToString("0.0"),
            AvgRating = "4.8",
            AvgDeliveryMinutes = 28,
            TotalDistanceKm = totalDistance.ToString("N1"),
            Ranking = new DriverRankingDto
            {
                Position = 12,
                TotalDrivers = 450,
                BadgeName = "Tài xế Xuất sắc Vàng"
            },
            ChartData = chartData
        };
    }

    /// <summary>
    /// Lấy dữ liệu biểu đồ biến động thu nhập (WEEK hoặc MONTH) của tài xế.
    /// </summary>
    public async Task<EarningsChartResponse> GetEarningsChartAsync(int driverId, string period)
    {
        var nowUtc = DateTime.UtcNow;
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, VietNamZone);
        List<EarningsChartItemDto> chartData = [];

        // Hàm tính netIncome chuẩn cho mỗi đơn: ShippingFee + Tip - (ShippingFee * 0.02)
        decimal CalcNet(decimal fee, decimal tip) => fee + tip - (fee * 0.02m);

        if (period.Equals("WEEK", StringComparison.OrdinalIgnoreCase))
        {
            // Lấy thứ 2 tuần này
            int diff = (int)nowVn.DayOfWeek - 1;
            if (diff < 0) diff = 6;
            var startVn = nowVn.Date.AddDays(-diff);

            string[] labels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
            for (int i = 0; i < 7; i++)
            {
                var dayStartVn = startVn.AddDays(i);
                var dayEndVn = dayStartVn.AddDays(1);
                var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartVn, VietNamZone);
                var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(dayEndVn, VietNamZone);

                var dayOrders = await _db.Orders.AsNoTracking()
                    .Where(o => o.DriverId == driverId
                             && o.Status == OrderStatus.COMPLETED
                             && o.CompletedAtUtc >= dayStartUtc
                             && o.CompletedAtUtc < dayEndUtc)
                    .Select(o => new { o.ShippingFee, o.TipAmount })
                    .ToListAsync();

                decimal totalIncome = dayOrders.Sum(o => CalcNet(o.ShippingFee, o.TipAmount));

                chartData.Add(new EarningsChartItemDto { Label = labels[i], Income = totalIncome });
            }
        }
        else // MONTH
        {
            var startVn = new DateTime(nowVn.Year, nowVn.Month, 1);
            for (int week = 1; week <= 4; week++)
            {
                var wStartVn = startVn.AddDays((week - 1) * 7);
                var wEndVn = week == 4 ? startVn.AddMonths(1) : wStartVn.AddDays(7);
                var wStartUtc = TimeZoneInfo.ConvertTimeToUtc(wStartVn, VietNamZone);
                var wEndUtc = TimeZoneInfo.ConvertTimeToUtc(wEndVn, VietNamZone);

                var weekOrders = await _db.Orders.AsNoTracking()
                    .Where(o => o.DriverId == driverId
                             && o.Status == OrderStatus.COMPLETED
                             && o.CompletedAtUtc >= wStartUtc
                             && o.CompletedAtUtc < wEndUtc)
                    .Select(o => new { o.ShippingFee, o.TipAmount })
                    .ToListAsync();

                decimal totalIncome = weekOrders.Sum(o => CalcNet(o.ShippingFee, o.TipAmount));

                chartData.Add(new EarningsChartItemDto { Label = $"Tuần {week}", Income = totalIncome });
            }
        }

        return new EarningsChartResponse
        {
            Period = period.ToUpper(),
            ChartData = chartData
        };
    }
}