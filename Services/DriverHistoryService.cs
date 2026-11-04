using Logistics.Api.Data;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services;

public class DriverHistoryService
{
    private readonly AppDbContext _db;
    private static readonly TimeZoneInfo VietNamZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public DriverHistoryService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 1. Lấy danh sách lịch sử đơn hàng có filter, search, sort và phân trang.
    /// </summary>
    public async Task<DriverHistoryListResponse> GetHistoryOrdersAsync(
        int driverId, string? status, string? q, string? sort, int page, int size, DateTime? from, DateTime? to)
    {
        var query = _db.Orders.AsNoTracking()
            .Where(o => o.DriverId == driverId);

        // Filter Status
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            {
                query = query.Where(o => o.Status == orderStatus);
            }
        }

        // Search (q) trên trackingCode và các địa chỉ
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(o =>
                o.TrackingCode.ToLower().Contains(term) ||
                o.PickupAddress.ToLower().Contains(term) ||
                o.DropoffAddress.ToLower().Contains(term));
        }

        // Filter Date Range
        if (from.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc <= to.Value);
        }

        // Sort
        query = sort?.ToLower() switch
        {
            "oldest" => query.OrderBy(o => o.CreatedAtUtc),
            "fee_desc" => query.OrderByDescending(o => o.ShippingFee),
            _ => query.OrderByDescending(o => o.CreatedAtUtc) // default 'latest'
        };

        var totalElements = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalElements / (double)size);

        var pagedOrders = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        var items = pagedOrders.Select(o => MapToHistoryItemDto(o)).ToList();

        return new DriverHistoryListResponse
        {
            Items = items,
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
    /// 2. Lấy KPI tổng quan của tài xế.
    /// </summary>
    public async Task<DriverHistoryKpiResponse> GetHistoryKpiAsync(int driverId)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.DriverId == driverId)
            .ToListAsync();

        var finishedOrders = orders.Where(o => o.Status == OrderStatus.COMPLETED || o.Status == OrderStatus.FAILED).ToList();
        var completedOrders = orders.Where(o => o.Status == OrderStatus.COMPLETED).ToList();
        var failedOrders = orders.Where(o => o.Status == OrderStatus.FAILED).ToList();

        int totalFinished = finishedOrders.Count;
        double completionRate = totalFinished > 0 ? (double)completedOrders.Count / totalFinished * 100 : 0;
        double failureRate = totalFinished > 0 ? (double)failedOrders.Count / totalFinished * 100 : 0;

        double avgRating = 5.0;

        var deliveryTimes = completedOrders
            .Where(o => o.PickedUpAtUtc.HasValue && o.CompletedAtUtc.HasValue)
            .Select(o => (o.CompletedAtUtc!.Value - o.PickedUpAtUtc!.Value).TotalMinutes)
            .ToList();

        int avgDeliveryMinutes = deliveryTimes.Count > 0 ? (int)Math.Round(deliveryTimes.Average()) : 32;

        double totalDistance = completedOrders.Sum(o => o.DistanceKm);

        return new DriverHistoryKpiResponse
        {
            TotalFinished = totalFinished,
            CompletionRate = completionRate.ToString("0.0"),
            FailureRate = failureRate.ToString("0.0"),
            AvgRating = avgRating.ToString("0.0"),
            AvgDeliveryMinutes = avgDeliveryMinutes,
            TotalDistance = totalDistance.ToString("N1")
        };
    }

    /// <summary>
    /// 3. Lấy dữ liệu biểu đồ (WEEK hoặc MONTH).
    /// </summary>
    public async Task<DriverHistoryChartResponse> GetHistoryChartAsync(int driverId, string period)
    {
        var nowUtc = DateTime.UtcNow;
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, VietNamZone);
        List<ChartItemDto> chartData = [];

        if (period.Equals("WEEK", StringComparison.OrdinalIgnoreCase))
        {
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

                var completedCount = await _db.Orders.CountAsync(o => o.DriverId == driverId && o.Status == OrderStatus.COMPLETED && o.CreatedAtUtc >= dayStartUtc && o.CreatedAtUtc < dayEndUtc);
                var failedCount = await _db.Orders.CountAsync(o => o.DriverId == driverId && (o.Status == OrderStatus.FAILED || o.Status == OrderStatus.CANCELLED) && o.CreatedAtUtc >= dayStartUtc && o.CreatedAtUtc < dayEndUtc);

                chartData.Add(new ChartItemDto { Label = labels[i], Completed = completedCount, Failed = failedCount });
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

                var completedCount = await _db.Orders.CountAsync(o => o.DriverId == driverId && o.Status == OrderStatus.COMPLETED && o.CreatedAtUtc >= wStartUtc && o.CreatedAtUtc < wEndUtc);
                var failedCount = await _db.Orders.CountAsync(o => o.DriverId == driverId && (o.Status == OrderStatus.FAILED || o.Status == OrderStatus.CANCELLED) && o.CreatedAtUtc >= wStartUtc && o.CreatedAtUtc < wEndUtc);

                chartData.Add(new ChartItemDto { Label = $"Tuần {week}", Completed = completedCount, Failed = failedCount });
            }
        }

        return new DriverHistoryChartResponse
        {
            Period = period.ToUpper(),
            ChartData = chartData
        };
    }

    /// <summary>
    /// 4. Lấy chi tiết một đơn hàng trong lịch sử.
    /// </summary>
    public async Task<DriverOrderDetailResponse> GetOrderDetailAsync(int driverId, int orderId)
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.DriverId == driverId)
            ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng hoặc bạn không có quyền xem."));

        var baseItem = MapToHistoryItemDto(order);

        string phone = order.ReceiverPhone;
        if (order.CompletedAtUtc.HasValue && (DateTime.UtcNow - order.CompletedAtUtc.Value).TotalDays > 7)
        {
            if (phone.Length > 6)
            {
                phone = phone[..4] + "***" + phone[^3..];
            }
        }

        // Xây dựng danh sách Timeline chuẩn cú pháp C#
        var timelineList = new List<OrderTimelineItemDto>
        {
            new() { Status = "CREATED", At = FormatIsoDate(order.CreatedAtUtc) },
            new() { Status = "ACCEPTED", At = FormatIsoDate(order.UpdatedAtUtc) }
        };

        if (order.PickedUpAtUtc.HasValue)
        {
            timelineList.Add(new OrderTimelineItemDto { Status = "PICKED_UP", At = FormatIsoDate(order.PickedUpAtUtc) });
        }

        if (order.CompletedAtUtc.HasValue)
        {
            timelineList.Add(new OrderTimelineItemDto { Status = "COMPLETED", At = FormatIsoDate(order.CompletedAtUtc) });
        }

        if (order.FailedAtUtc.HasValue)
        {
            timelineList.Add(new OrderTimelineItemDto { Status = "FAILED", At = FormatIsoDate(order.FailedAtUtc) });
        }

        return new DriverOrderDetailResponse
        {
            Id = baseItem.Id,
            TrackingCode = baseItem.TrackingCode,
            Status = baseItem.Status,
            SenderAddress = baseItem.SenderAddress,
            PickupAddress = baseItem.PickupAddress,
            PickupLat = order.PickupLat,
            PickupLng = order.PickupLng,
            ReceiverAddress = baseItem.ReceiverAddress,
            DropoffLat = order.DropoffLat,
            DropoffLng = order.DropoffLng,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = phone,
            PackageName = baseItem.PackageName,
            WeightKg = baseItem.WeightKg,
            ShippingFee = baseItem.ShippingFee,
            VehicleType = baseItem.VehicleType,
            ServiceType = baseItem.ServiceType,
            PaymentMethod = baseItem.PaymentMethod,
            CodAmount = baseItem.CodAmount,
            DistanceKm = baseItem.DistanceKm,
            CreatedAt = baseItem.CreatedAt,
            CompletedAt = baseItem.CompletedAt,
            ReviewRating = baseItem.ReviewRating,
            ReviewComment = baseItem.ReviewComment,
            FailureReason = baseItem.FailureReason,
            IsBulky = order.IsBulky,
            IsDoorDelivery = order.IsDoorDelivery,
            AcceptedAt = FormatIsoDate(order.UpdatedAtUtc),
            PickedUpAt = FormatIsoDate(order.PickedUpAtUtc),
            FailedAt = FormatIsoDate(order.FailedAtUtc),
            Timeline = timelineList
        };
    }

    private HistoryOrderItemDto MapToHistoryItemDto(DeliveryOrder o)
    {
        return new HistoryOrderItemDto
        {
            Id = $"ord_hist_{o.Id}",
            TrackingCode = o.TrackingCode,
            Status = o.Status.ToString(),
            SenderAddress = o.PickupAddress,
            PickupAddress = o.PickupAddress,
            PickupLat = o.PickupLat,
            ReceiverAddress = o.DropoffAddress,
            DropoffLat = o.DropoffLat,
            ReceiverName = o.ReceiverName,
            ReceiverPhone = o.ReceiverPhone,
            PackageName = o.PackageName,
            WeightKg = o.WeightKg,
            ShippingFee = o.ShippingFee,
            VehicleType = o.VehicleType.ToString(),
            ServiceType = o.ServiceType,
            PaymentMethod = o.PaymentMethod.ToString(),
            CodAmount = o.CodAmount,
            DistanceKm = o.DistanceKm,
            CreatedAt = FormatIsoDate(o.CreatedAtUtc),
            CompletedAt = FormatIsoDate(o.CompletedAtUtc),
            ReviewRating = 5.0,
            ReviewComment = o.Status == OrderStatus.COMPLETED ? "Tài xế giao hàng rất nhanh nhẹn và lịch sự!" : null,
            FailureReason = o.FailureReason
        };
    }

    private string FormatIsoDate(DateTime? utcDate)
    {
        if (!utcDate.HasValue) return "";
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(utcDate.Value, VietNamZone);
        return vnTime.ToString("yyyy-MM-ddTHH:mm:ss+07:00");
    }
}