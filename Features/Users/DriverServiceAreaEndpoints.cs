using Logistics.Api.Common.Filters;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Services;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api.Features.Drivers;

public static class DriverServiceAreaEndpoints
{
    public static void MapDriverServiceAreaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/driver/service-areas")
                       .RequireAuthorization()
                       .AddEndpointFilter<ModelValidationFilter>();

        // 1. Lấy danh sách vùng phục vụ của tài xế
        group.MapGet("/", async (ClaimsPrincipal p, DriverServiceAreaService svc) =>
        {
            int driverId = UserId(p);
            var result = await svc.GetMyServiceAreasAsync(driverId);
            return Results.Ok(ApiResponse<object>.Success(result, "Lấy danh sách vùng phục vụ thành công"));
        });

        // 2. Thêm vùng phục vụ mới
        group.MapPost("/", async (UpsertServiceAreaRequest req, ClaimsPrincipal p, DriverServiceAreaService svc) =>
        {
            int driverId = UserId(p);
            var result = await svc.CreateServiceAreaAsync(driverId, req);
            return Results.Ok(ApiResponse<object>.Success(result, "Thêm vùng phục vụ thành công"));
        });

        // 3. Cập nhật vùng phục vụ
        group.MapPut("/{id:int}", async (int id, UpsertServiceAreaRequest req, ClaimsPrincipal p, DriverServiceAreaService svc) =>
        {
            int driverId = UserId(p);
            var result = await svc.UpdateServiceAreaAsync(id, driverId, req);
            return Results.Ok(ApiResponse<object>.Success(result, "Cập nhật vùng phục vụ thành công"));
        });

        // 4. Xóa vùng phục vụ
        group.MapDelete("/{id:int}", async (int id, ClaimsPrincipal p, DriverServiceAreaService svc) =>
        {
            int driverId = UserId(p);
            await svc.DeleteServiceAreaAsync(id, driverId);
            return Results.Ok(ApiResponse<object>.Success(null, "Xóa vùng phục vụ thành công"));
        });
    }
}