using Logistics.Api.Common.Filters;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Mappings;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Exceptions;
using Logistics.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api.Features.Orders;

/// <summary>
/// Lớp định nghĩa Minimal API Endpoints cho Đơn hàng.
/// </summary>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/orders").RequireAuthorization();

        // 5. Tạo đơn hàng mới (Khách hàng)
        api.MapPost("/", async (CreateOrderRequest req, ClaimsPrincipal p, OrderService orderService) =>
        {

            Console.WriteLine($"[FILTER] Called with {Role(p)} args");
            if (Role(p).ToString().ToUpper() != UserRole.Customer)
            {

                // Ném ngoại lệ chuẩn thay vì trả về Result.Forbid() thủ công để đồng bộ ApiResponse
                throw new AppException(OrderError.ForbiddenCustomer);
            }

            // Gọi Service thực hiện tạo đơn (Service sẽ tự động throw AppException nếu sai GPS hoặc thiếu thông tin)
            var order = await orderService.CreateOrderAsync(UserId(p), req);

            // Trả về thành công theo cấu trúc ApiResponse chuẩn: { "status": "ok", "message": "...", "data": {...} }
            return Results.Ok(ApiResponse<object>.Success(order.ToResponse(), "Tạo đơn hàng thành công"));
        });

        // 2. API cho Customer: Lấy danh sách đơn hàng của chính khách hàng đang đăng nhập
        api.MapGet("/my-orders", async (int? page, int? itemPerpage, string? search,string? status, ClaimsPrincipal p, OrderService orderService) =>
        {
            if (Role(p).ToString().ToUpper() != UserRole.Customer)
            {
                throw new AppException(new OrderError(403, "Chỉ có khách hàng mới có quyền xem danh sách đơn hàng cá nhân."));
            }

            int pg = page ?? 1;
            int limit = itemPerpage ?? 10;
            int customerId = UserId(p);

            var result = await orderService.GetCustomerOrdersAsync(customerId, pg, limit, search,status);
            return Results.Ok(ApiResponse<object>.Success(result, "Lấy danh sách đơn hàng cá nhân thành công"));
        });

        // 3. API cho Admin: Lấy tất cả đơn hàng có trong cơ sở dữ liệu
        api.MapGet("/manager", async (int? page, int? itemPerpage, string? search,string?status ,ClaimsPrincipal p, OrderService orderService) =>
        {
            // Kiểm tra quyền Admin (hoặc bạn có thể tùy chỉnh lại logic check role Admin của hệ thống)
            var userRole = Role(p).ToString().ToUpper();
            if (userRole != UserRole.Admin)
            {
                throw new AppException(new OrderError(403, "Bạn không có quyền truy cập chức năng này."));
            }

            int pg = page ?? 1;
            int limit = itemPerpage ?? 10;

            var result = await orderService.GetAllOrdersForAdminAsync(pg, limit, search,status);
            return Results.Ok(ApiResponse<object>.Success(result, "Lấy tất cả đơn hàng hệ thống thành công"));
        });


        // 4. Lấy chi tiết đơn hàng theo ID
        api.MapGet("/{id:int}", async (int id, ClaimsPrincipal p, OrderService orderService) =>
        {
            int userId = UserId(p);
            string userRole = Role(p).ToString();

            // Gọi service lấy dữ liệu (Service sẽ tự throw AppException nếu không thấy hoặc trái phép)
            var order = await orderService.GetOrderByIdAsync(id, userId, userRole);

            // Áp dụng extension method .ToResponse() có sẵn để trả về định dạng OrderResponse chuẩn
            var responseData = order.ToResponse();

            return Results.Ok(ApiResponse<object>.Success(responseData, "Lấy thông tin chi tiết đơn hàng thành công"));
        });



    }
}