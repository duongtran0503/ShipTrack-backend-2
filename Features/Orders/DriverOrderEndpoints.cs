using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Exceptions;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api.Features.Orders;

public static class DriverOrderEndpoints
{
    public static void MapDriverOrderEndpoints(this IEndpointRouteBuilder app)
    {
        // Nhóm API dành riêng cho Driver, bắt buộc đăng nhập
        var group = app.MapGroup("/api/driver/orders")
                       .RequireAuthorization();

        // API: Lấy danh sách đơn hàng khả dụng xung quanh vị trí của tài xế
        group.MapGet("/available", async (
            ClaimsPrincipal p,
            [FromServices] DriverMatchingService matchingService) =>
        {
            string userRole = Role(p).ToUpper();

            // Kiểm tra quyền: Chỉ tài xế mới được gọi API này
            if (!userRole.StartsWith("DRIVER"))
            {
                throw new AppException(new AuthError(403, "Chỉ có tài xế mới có quyền xem danh sách đơn hàng khả dụng."));
            }

            int driverId = UserId(p);

            // Gọi service xử lý logic tìm đơn (đã tích hợp Haversine, Bounding Box và Vehicle/Service Area)
            var availableOrders = await matchingService.GetAvailableOrdersAsync(driverId);

            return Results.Ok(ApiResponse<object>.Success(availableOrders, "Lấy danh sách đơn hàng khả dụng thành công"));
        });

        // 2. Xem chi tiết đơn hàng chưa nhận (GET Detail)
        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal p, [FromServices] DriverMatchingService matchingService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem chi tiết đơn hàng."));

            int driverId = UserId(p);
            var result = await matchingService.GetOrderDetailAsync(driverId, id);
            return Results.Ok(ApiResponse<OrderDetailResponse>.Success(result, "Lấy chi tiết đơn hàng thành công"));
        });

        // 3. Nhận đơn hàng (Accept Order)
        group.MapPost("/{id:int}/accept", async (int id, ClaimsPrincipal p, [FromServices] DriverMatchingService matchingService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền nhận đơn hàng."));

            int driverId = UserId(p);
            var result = await matchingService.AcceptOrderAsync(driverId, id);
            return Results.Ok(ApiResponse<OrderDetailResponse>.Success(result, "Nhận đơn hàng thành công"));
        });

       

        // 4. Cập nhật trạng thái: Đã lấy hàng (PICKED_UP)
        group.MapPatch("/{id:int}/picked-up", async (int id, ClaimsPrincipal p, [FromServices] DriverOrderService driverOrderService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền cập nhật trạng thái này."));

            int driverId = UserId(p);
            var result = await driverOrderService.MarkAsPickedUpAsync(driverId, id);
            return Results.Ok(ApiResponse<OrderDetailResponse>.Success(result, "Xác nhận đã lấy hàng thành công"));
        });

        // 5. Cập nhật trạng thái: Đang giao hàng (DELIVERING)
        group.MapPatch("/{id:int}/delivering", async (int id, ClaimsPrincipal p, [FromServices] DriverOrderService driverOrderService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền cập nhật trạng thái này."));

            int driverId = UserId(p);
            var result = await driverOrderService.MarkAsDeliveringAsync(driverId, id);
            return Results.Ok(ApiResponse<OrderDetailResponse>.Success(result, "Cập nhật trạng thái đang giao hàng thành công"));
        });

        // 6. Cập nhật trạng thái: Hoàn thành đơn hàng (COMPLETED)
        group.MapPost("/{id:int}/complete", async (int id, CompleteOrderRequest req, ClaimsPrincipal p, [FromServices] DriverOrderService driverOrderService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền hoàn thành đơn hàng."));

            int driverId = UserId(p);
            var result = await driverOrderService.MarkAsCompletedAsync(driverId, id, req?.ProofPhotoPath);
            return Results.Ok(ApiResponse<OrderDetailResponse>.Success(result, "Hoàn thành đơn hàng thành công"));
        });

        // 7. Lấy danh sách đơn hàng đang thực hiện
        group.MapGet("/active", async (ClaimsPrincipal p, [FromServices] DriverMatchingService matchingService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem danh sách đơn đang thực hiện."));

            int driverId = UserId(p);
            var result = await matchingService.GetActiveOrdersForDriverAsync(driverId);
            return Results.Ok(ApiResponse<object>.Success(result, "Lấy danh sách đơn hàng đang thực hiện thành công"));
        });

        // 8. Lấy lịch sử đơn hàng đã hoàn thành / hủy
        group.MapGet("/history", async ([AsParameters] PaginationAndFilterRequest query, ClaimsPrincipal p, [FromServices] DriverMatchingService matchingService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem lịch sử đơn hàng."));

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;
            int driverId = UserId(p);
            var (items, totalCount) = await matchingService.GetDriverOrderHistoryAsync(driverId, pageNumber, pageSize);

            var responseData = new
            {
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                Items = items
            };

            return Results.Ok(ApiResponse<object>.Success(responseData, "Lấy lịch sử đơn hàng thành công"));
        });

        // 9. Xem chi tiết đơn hàng đã nhận / đang thực hiện
        group.MapGet("/{id:int}/active-detail", async (int id, ClaimsPrincipal p, [FromServices] DriverMatchingService matchingService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền xem chi tiết đơn hàng này."));

            int driverId = UserId(p);
            var result = await matchingService.GetActiveOrderDetailAsync(driverId, id);
            return Results.Ok(ApiResponse<OrderDetailResponse>.Success(result, "Lấy chi tiết đơn hàng đang thực hiện thành công"));
        });

        // 10. Cập nhật trạng thái: Thất bại / Không giao được (FAILED)
        group.MapPatch("/{id:int}/fail", async (int id, FailOrderRequest req, ClaimsPrincipal p, [FromServices] DriverOrderService orderService) =>
        {
            string userRole = Role(p).ToUpper();
            if (!userRole.StartsWith("DRIVER"))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có quyền cập nhật trạng thái này."));

            int driverId = UserId(p);
            var result = await orderService.MarkAsFailedAsync(driverId, id, req?.FailureReason);
            return Results.Ok(ApiResponse<OrderDetailResponse>.Success(result, "Cập nhật trạng thái đơn hàng thất bại thành công"));
        });
    }
}