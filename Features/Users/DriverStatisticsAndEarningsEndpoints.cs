using Logistics.Api.Domain.Exceptions;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api.Features.Users;

public static class DriverStatisticsAndEarningsEndpoints
{
    public static void MapDriverStatisticsAndEarningsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/drivers/me")
                       .RequireAuthorization();

        // 1. API Thu nhập tài xế (useDriverEarnings)
        group.MapGet("/earnings", async (
            [FromQuery] string range,
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] string? paymentMethod,
            ClaimsPrincipal p,
            [FromServices] DriverStatisticsAndEarningsService svc) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem thông tin thu nhập."));

            if (string.IsNullOrWhiteSpace(range))
                throw new AppException(new AuthError(400, "Tham số range là bắt buộc (TODAY, THIS_WEEK, THIS_MONTH)."));

            int driverId = UserId(p);
            int pageNumber = page ?? 0;
            int pageSize = size ?? 20;

            var result = await svc.GetDriverEarningsAsync(driverId, range, pageNumber, pageSize, paymentMethod);
            return Results.Ok(result);
        });

        // 2. API Thống kê cá nhân (useDriverStatistics)
        group.MapGet("/statistics", async (
            [FromQuery] string period,
            ClaimsPrincipal p,
            [FromServices] DriverStatisticsAndEarningsService svc) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem thống kê cá nhân."));

            if (string.IsNullOrWhiteSpace(period))
                throw new AppException(new AuthError(400, "Tham số period là bắt buộc (WEEK, MONTH)."));

            int driverId = UserId(p);
            var result = await svc.GetDriverStatisticsAsync(driverId, period);
            return Results.Ok(result);
        });

        // 3. API Biểu đồ biến động thu nhập (Dành cho EarningsChartCard)
        group.MapGet("/earnings/chart", async (
            [FromQuery] string? period,
            ClaimsPrincipal p,
            [FromServices] DriverStatisticsAndEarningsService svc) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem biểu đồ thu nhập."));

            string selectedPeriod = string.IsNullOrWhiteSpace(period) ? "WEEK" : period;
            int driverId = UserId(p);

            var result = await svc.GetEarningsChartAsync(driverId, selectedPeriod);
            return Results.Ok(result);
        });
    }
}