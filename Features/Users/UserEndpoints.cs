using Logistics.Api.Domain;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Mappings;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;
namespace Logistics.Api.Features.Users;

/// <summary>
/// Lớp định nghĩa các Minimal API Endpoints liên quan đến người dùng, ví tiền, tọa độ tài xế và hộp thư hỗ trợ.
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/users").RequireAuthorization();

        // API lấy thông tin cá nhân hiện tại
        api.MapGet("/me", async (ClaimsPrincipal p, UserService userService) =>
        {
            // 1. Lấy userId từ token
            int userId = UserId(p);

            // 2. Giao toàn bộ việc xử lý logic cho Service
            var profile = await userService.GetMyProfileAsync(userId);

            // 3. Trả về kết quả bọc trong ApiResponse chuẩn
            return Results.Ok(ApiResponse<object>.Success(profile, "Lấy thông tin cá nhân thành công"));
        });

        // POST /api/users/me/change-password
        api.MapPost("/me/change-password", async (
            ChangePasswordRequest req,
            ClaimsPrincipal p,
            UserService userService) =>
        {
            await userService.ChangePasswordAsync(UserId(p), req);
            return Results.Ok(ApiResponse<object>.Success(null, "Đổi mật khẩu thành công"));
        });

        // PUT /api/users/
        api.MapPut("/me", async (
     
            UpdateUserRequest req,
            ClaimsPrincipal p,
            UserService userService) =>
        {
            // 1. Lấy userId từ token
            int userId = UserId(p);
            var user = await userService.UpdateAsync(userId, req);
            return Results.Ok(ApiResponse<object>.Success(user, "Cập nhật thành công"));
        });


        api.MapGet("/dashboard-stats", async (
          ClaimsPrincipal p,
          UserService userService
         ) =>
        {
            // Chỉ Customer mới được truy cập
            if (Role(p) != UserRole.Customer)
                return Results.Forbid();

            var stats = await userService.GetDashboardStatsAsync(UserId(p)) ;

            return Results.Ok(ApiResponse<CustomerDashboardStatsResponse>.Success(
                stats,
                "Lấy thống kê thành công"));
        });

        // GET /api/v1/customer/orders/{id} -> Lấy chi tiết đơn hàng của khách hàng
        api.MapGet("/customer/orders/{id:int}", async (
            int id,
            ClaimsPrincipal p,
            [FromServices] OrderService orderService) =>
        {
            string userRole = Role(p).ToUpper();
            if (userRole != "CUSTOMER")
            {
                return Results.Forbid();
            }

            int customerId = UserId(p);

            // Gọi hàm đã có sẵn trong OrderService để lấy đơn kèm phân quyền
            var order = await orderService.GetCustomerOrderDetailAsync(id, customerId);

            // Chuyển đổi sang Response DTO (đã tự động include DriverBriefResponse nếu có driver nhận)
            

            return Results.Ok(ApiResponse<OrderResponse>.Success(order, "Lấy thông tin chi tiết đơn hàng thành công"));
        });
    }



    // --- CÁC HÀM TIỆN ÍCH (HELPER METHODS) ---

    // Trả về định dạng lỗi JSON chuẩn
    private static IResult Error(string message, int status = 400) =>
        Results.Json(new { detail = message }, statusCode: status);

    // Kiểm tra tọa độ GPS hợp lệ (vĩ độ -90 đến 90, kinh độ -180 đến 180)
    private static bool ValidGps(double lat, double lng) =>
        double.IsFinite(lat) && double.IsFinite(lng) && lat is >= -90 and <= 90 && lng is >= -180 and <= 180;
}