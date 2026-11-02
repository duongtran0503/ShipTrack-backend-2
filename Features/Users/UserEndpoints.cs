using System.Security.Claims;
using Logistics.Api.Domain;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Features.Users;

/// <summary>
/// Lớp định nghĩa các Minimal API Endpoints liên quan đến người dùng, ví tiền, tọa độ tài xế và hộp thư hỗ trợ.
/// </summary>
public static class UserEndpoints
{
    //public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    //{
    //    // Gom nhóm các route bắt đầu với tiền tố /api và yêu cầu phải xác thực JWT
    //    var api = app.MapGroup("/api").RequireAuthorization();

    //    // 1. Lấy thông tin cá nhân của user hiện tại đang đăng nhập
    //    api.MapGet("/users/me", async (ClaimsPrincipal p, UserService userService) =>
    //    {
    //        var u = await userService.GetUserByIdAsync(UserId(p));
    //        return u == null ? Results.NotFound() : Results.Ok(UserView(u));
    //    });

    //    // 2. Lấy thông tin chi tiết user theo ID (Chính chủ hoặc Admin mới có quyền xem)
    //    api.MapGet("/users/{id:int}", async (int id, ClaimsPrincipal p, UserService userService) =>
    //    {
    //        if (UserId(p) != id && Role(p) != "admin")
    //            return Results.Forbid();

    //        var u = await userService.GetUserByIdAsync(id);
    //        return u == null ? Results.NotFound() : Results.Ok(UserView(u));
    //    });

    //    // 3. Cập nhật thông tin cá nhân / profile của user hiện tại
    //    api.MapPut("/users/me", async (ProfileRequest req, ClaimsPrincipal p, UserService userService) =>
    //    {
    //        var u = await userService.UpdateProfileAsync(UserId(p), req);
    //        if (u == null)
    //            return Error("Tên không được trống hoặc không tìm thấy user.");

    //        return Results.Ok(UserView(u));
    //    });

    //    // 4. Cập nhật vị trí GPS trực tiếp và trạng thái online của tài xế
    //    api.MapPut("/drivers/me/location", async (LocationRequest req, ClaimsPrincipal p, UserService userService) =>
    //    {
    //        if (!Role(p).StartsWith("driver"))
    //            return Results.Forbid();

    //        if (!ValidGps(req.Latitude, req.Longitude))
    //            return Error("Tọa độ không hợp lệ.");

    //        var u = await userService.UpdateDriverLocationAsync(UserId(p), req);
    //        if (u == null)
    //            return Results.NotFound();

    //        return Results.Ok(new { online = u.Online, latitude = u.Latitude, longitude = u.Longitude });
    //    });

    //    // 5. Lấy lịch sử giao dịch ví của người dùng (Chính chủ hoặc Admin)
    //    api.MapGet("/users/{id:int}/transactions", async (int id, ClaimsPrincipal p, UserService userService) =>
    //    {
    //        if (id != UserId(p) && Role(p) != "admin")
    //            return Results.Forbid();

    //        var rows = await userService.GetUserTransactionsAsync(id);
    //        return Results.Ok(rows.Select(t => new {
    //            id = t.Id,
    //            type = t.Type,
    //            amount = t.Amount,
    //            title = t.Description,
    //            description = t.Description,
    //            timestamp = t.CreatedAtUtc,
    //            created_at = t.CreatedAtUtc,
    //            createdAt = t.CreatedAtUtc,
    //            order_id = t.OrderId
    //        }));
    //    });

    //    // 6. Nạp tiền demo vào ví (Chỉ hoạt động ở môi trường Development)
    //    api.MapPost("/users/{id:int}/topup", async (int id, AmountRequest req, ClaimsPrincipal p, UserService userService) =>
    //    {
    //        if (id != UserId(p) && Role(p) != "admin")
    //            return Results.Forbid();

    //        var (success, err, user) = await userService.TopUpAsync(id, req.Amount);
    //        if (!success)
    //            return Error(err ?? "Không thể nạp tiền.", err?.Contains("Development") == true ? 403 : 400);

    //        return Results.Ok(UserView(user!));
    //    });

    //    // 7. Rút tiền demo khỏi ví (Kiểm tra số dư và môi trường Development)
    //    api.MapPost("/users/{id:int}/withdraw", async (int id, AmountRequest req, ClaimsPrincipal p, UserService userService) =>
    //    {
    //        if (id != UserId(p) && Role(p) != "admin")
    //            return Results.Forbid();

    //        var (success, err, user) = await userService.WithdrawAsync(id, req.Amount);
    //        if (!success)
    //            return Error(err ?? "Không thể rút tiền.", err?.Contains("Development") == true ? 403 : (err?.Contains("đủ") == true ? 409 : 400));

    //        return Results.Ok(UserView(user!));
    //    });

    //    // 8. Lấy danh sách tin nhắn hỗ trợ (Support) của người dùng
    //    api.MapGet("/support/{id:int}/messages", async (int id, ClaimsPrincipal p, UserService userService) =>
    //    {
    //        if (id != UserId(p) && Role(p) != "admin")
    //            return Results.Forbid();

    //        var rows = await userService.GetSupportMessagesAsync(id);
    //        return Results.Ok(rows.Select(m => new {
    //            id = m.Id,
    //            user_id = m.UserId,
    //            sender_type = m.SenderType,
    //            content = m.Content,
    //            created_at = m.CreatedAtUtc
    //        }));
    //    });

    //    // 9. Gửi yêu cầu/tin nhắn hỗ trợ mới đến hệ thống
    //    api.MapPost("/support/messages", async (SupportRequest req, ClaimsPrincipal p, UserService userService) =>
    //    {
    //        if (string.IsNullOrWhiteSpace(req.Content) || req.Content.Length > 2000)
    //            return Error("Nội dung không hợp lệ.");

    //        await userService.SendSupportMessageAsync(UserId(p), req.Content);
    //        return Results.Ok(new { message = "Đã gửi yêu cầu hỗ trợ." });
    //    });
    //}

    // --- CÁC HÀM TIỆN ÍCH (HELPER METHODS) ---

    // Trả về định dạng lỗi JSON chuẩn
    private static IResult Error(string message, int status = 400) =>
        Results.Json(new { detail = message }, statusCode: status);

    // Kiểm tra tọa độ GPS hợp lệ (vĩ độ -90 đến 90, kinh độ -180 đến 180)
    private static bool ValidGps(double lat, double lng) =>
        double.IsFinite(lat) && double.IsFinite(lng) && lat is >= -90 and <= 90 && lng is >= -180 and <= 180;
}