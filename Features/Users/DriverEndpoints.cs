using Logistics.Api.Common.Filters;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;
namespace Logistics.Api.Features.Users
{
    public static class DriverEndpoints
    {
        public static void MapDriverEndpoints(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("/api/drivers")
                .RequireAuthorization()
                .AddEndpointFilter<ModelValidationFilter>();

            // PATCH /api/driver/status — bật/tắt online
            api.MapPatch("/status", async (
                UpdateDriverStatusRequest req,
                ClaimsPrincipal p,
                DriverService svc) =>
            {
                var role = Role(p);
                if (!role.StartsWith("DRIVER", StringComparison.OrdinalIgnoreCase))
                    return Results.Forbid();

                var result = await svc.UpdateStatusAsync(UserId(p), req.IsOnline);
                return Results.Ok(ApiResponse<object>.Success(
                    result,
                    req.IsOnline ? "Đã bật trạng thái sẵn sàng nhận đơn!" : "Đã chuyển sang ngoại tuyến."));
            });

            // GET /api/driver/dashboard-stats — lấy stats thật
            api.MapGet("/dashboard-stats", async (ClaimsPrincipal p, DriverService svc) =>
            {
                var role = Role(p);
                if (!role.StartsWith("DRIVER", StringComparison.OrdinalIgnoreCase))
                    return Results.Forbid();

                var stats = await svc.GetDashboardStatsAsync(UserId(p));
                return Results.Ok(ApiResponse<object>.Success(stats, "Lấy thống kê thành công"));
            });

            // GET /api/driver/status — lấy trạng thái hiện tại (khi mở app)
            api.MapGet("/status", async (ClaimsPrincipal p, DriverService svc) =>
            {
                var role = Role(p);
                if (!role.StartsWith("DRIVER", StringComparison.OrdinalIgnoreCase))
                    return Results.Forbid();

                var stats = await svc.GetDashboardStatsAsync(UserId(p));
                return Results.Ok(ApiResponse<object>.Success(stats, "OK"));
            });

            // [DRIVER] Cập nhật tọa độ GPS trực tiếp real-time
            api.MapPut("/location", async (UpdateLocationRequest req, ClaimsPrincipal p, [FromServices] DriverService driverService) =>
            {
                string userRole = Role(p).ToUpper();
                if (!userRole.StartsWith("DRIVER"))
                {
                    return Results.Forbid();
                }

                int driverId = UserId(p);
                await driverService.UpdateDriverLocationAsync(driverId, req.Latitude, req.Longitude);

                return Results.Ok(ApiResponse<object?>.Success(null, "Cập nhật vị trí thành công"));
            });
        }
    }
}
