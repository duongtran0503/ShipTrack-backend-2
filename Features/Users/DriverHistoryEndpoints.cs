using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Exceptions;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api.Features.Users;

public static class DriverHistoryEndpoints
{
    public static void MapDriverHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/drivers/me/orders")
                       .RequireAuthorization();

        // 1. GET /api/v1/drivers/me/orders/history
        group.MapGet("/history", async (
            [FromQuery] string? status,
            [FromQuery] string? q,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            ClaimsPrincipal p,
            [FromServices] DriverHistoryService svc) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem lịch sử đơn hàng."));

            int driverId = UserId(p);
            int pageNumber = page ?? 0;
            int pageSize = size ?? 20;

            var result = await svc.GetHistoryOrdersAsync(driverId, status, q, sort, pageNumber, pageSize, from, to);
            return Results.Ok(result);
        });

        // 2. GET /api/v1/drivers/me/orders/history/kpi
        group.MapGet("/history/kpi", async (
            ClaimsPrincipal p,
            [FromServices] DriverHistoryService svc) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem KPI lịch sử."));

            int driverId = UserId(p);
            var result = await svc.GetHistoryKpiAsync(driverId);
            return Results.Ok(result);
        });

        // 3. GET /api/v1/drivers/me/orders/history/chart
        group.MapGet("/history/chart", async (
            [FromQuery] string? period,
            ClaimsPrincipal p,
            [FromServices] DriverHistoryService svc) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem biểu đồ lịch sử."));

            string selectedPeriod = string.IsNullOrWhiteSpace(period) ? "WEEK" : period;
            int driverId = UserId(p);

            var result = await svc.GetHistoryChartAsync(driverId, selectedPeriod);
            return Results.Ok(result);
        });

        // 4. GET /api/v1/drivers/me/orders/{orderId}
        group.MapGet("/{orderId:int}", async (
            int orderId,
            ClaimsPrincipal p,
            [FromServices] DriverHistoryService svc) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem chi tiết đơn hàng."));

            int driverId = UserId(p);
            var result = await svc.GetOrderDetailAsync(driverId, orderId);
            return Results.Ok(result);
        });
    }
}