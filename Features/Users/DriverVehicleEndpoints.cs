using Logistics.Api.Common.Filters;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api.Features.Drivers;

public static class DriverVehicleEndpoints
{
    public static void MapDriverVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/driver/vehicles")
                       .RequireAuthorization()
                       .AddEndpointFilter<ModelValidationFilter>();

        // 1. Lấy danh sách phương tiện của tài xế
        group.MapGet("/", async (ClaimsPrincipal p, DriverVehicleService svc) =>
        {
            int driverId = UserId(p);
            var result = await svc.GetMyVehiclesAsync(driverId);
            return Results.Ok(ApiResponse<object>.Success(result, "Lấy danh sách phương tiện thành công"));
        });

        // 2. Thêm phương tiện mới (Đăng ký xe mới)
        group.MapPost("/", async (UpsertVehicleRequest req, ClaimsPrincipal p, DriverVehicleService svc) =>
        {
            int driverId = UserId(p);
            var result = await svc.CreateVehicleAsync(driverId, req);
            return Results.Ok(ApiResponse<object>.Success(result, "Thêm phương tiện mới thành công"));
        });

        // 3. Đổi xe mặc định (Chọn xe để chạy đơn)
        group.MapPost("/{id:int}/set-default", async (int id, ClaimsPrincipal p, DriverVehicleService svc) =>
        {
            int driverId = UserId(p);
            var result = await svc.SetDefaultVehicleAsync(id, driverId);
            return Results.Ok(ApiResponse<object>.Success(result, "Đã chọn phương tiện mặc định"));
        });

        // 4. Xóa phương tiện
        group.MapDelete("/{id:int}", async (int id, ClaimsPrincipal p, DriverVehicleService svc) =>
        {
            int driverId = UserId(p);
            await svc.DeleteVehicleAsync(id, driverId);
            return Results.Ok(ApiResponse<object>.Success(null, "Xóa phương tiện thành công"));
        });

        // 5. [ADMIN] Lấy danh sách tất cả phương tiện trong hệ thống (hỗ trợ phân trang, tìm kiếm và lọc)
        group.MapGet("/admin/all", async ([AsParameters] PaginationAndFilterRequest query, bool? isVerified, ClaimsPrincipal p, DriverVehicleService svc) =>
        {
            string role = Role(p).ToUpper();
            if (!role.Contains("ADMIN"))
            {
                return Results.Forbid();
            }
            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            var (items, totalCount) = await svc.AdminGetAllVehiclesAsync(
                isVerified, pageNumber, pageSize, query.SearchTerm);

            var responseData = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };

            return Results.Ok(ApiResponse<object>.Success(responseData, "Lấy danh sách phương tiện toàn hệ thống thành công"));
        });

        // 6. [ADMIN] Duyệt hoặc từ chối phương tiện của tài xế
        group.MapPatch("/admin/{id:int}/verify", async (int id, VerifyVehicleRequest req, ClaimsPrincipal p, DriverVehicleService svc) =>
        {
            string role = Role(p).ToUpper();
            if (!role.Contains("ADMIN"))
            {
                return Results.Forbid();
            }

            var result = await svc.VerifyVehicleAsync(id, req.IsVerified);
            string actionText = req.IsVerified ? "đã được duyệt" : "đã bị từ chối duyệt";

            return Results.Ok(ApiResponse<DriverVehicleResponse>.Success(result, $"Phương tiện {actionText} thành công"));
        });


        // [DRIVER] Lấy thông tin phương tiện đang sử dụng (Mặc định & Đã được duyệt)
        group.MapGet("/current-active", async (ClaimsPrincipal p, [FromServices] DriverVehicleService svc) =>
        {
            string role = Role(p).ToUpper();
            if (!role.StartsWith("DRIVER"))
            {
                return Results.Forbid();
            }

            int driverId = UserId(p);
            var result = await svc.GetCurrentActiveVehicleAsync(driverId);

            if (result == null)
            {
                return Results.Ok(ApiResponse<object?>.Success(null, "Tài xế hiện chưa có phương tiện nào được duyệt hoặc được chọn làm mặc định."));
            }

            return Results.Ok(ApiResponse<DriverVehicleResponse>.Success(result, "Lấy thông tin phương tiện đang sử dụng thành công"));
        });
    }
}