using Logistics.Api.Common.Filters;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Services;
using System.Security.Claims;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api.Features.Users;

public static class AddressEndpoints
{
    public static void MapAddressEndpoints(this IEndpointRouteBuilder app)
    {
        // Tất cả các API quản lý địa chỉ đều bắt buộc đăng nhập và gắn bộ lọc validate Model tự động
        var group = app.MapGroup("/api/addresses")
                       .RequireAuthorization()
                       .AddEndpointFilter<ModelValidationFilter>();

        // 1. Lấy danh sách sổ địa chỉ của user hiện tại
        group.MapGet("/", async (ClaimsPrincipal p, AddressService addressService) =>
        {
            int ownerId = UserId(p);
            var result = await addressService.GetMyAddressesAsync(ownerId);
            return Results.Ok(ApiResponse<object>.Success(result, "Lấy danh sách địa chỉ thành công"));
        });

        // 2. Lấy chi tiết 1 địa chỉ theo ID
        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal p, AddressService addressService) =>
        {
            int ownerId = UserId(p);
            var result = await addressService.GetAddressByIdAsync(id, ownerId);
            return Results.Ok(ApiResponse<object>.Success(result, "Lấy chi tiết địa chỉ thành công"));
        });

        // 3. Thêm mới địa chỉ vào sổ
        group.MapPost("/", async (UpsertAddressRequest req, ClaimsPrincipal p, AddressService addressService) =>
        {
            int ownerId = UserId(p);
            var result = await addressService.CreateAddressAsync(ownerId, req);
            return Results.Created($"/api/addresses/{result.Id}", ApiResponse<object>.Success(result, "Thêm địa chỉ thành công"));
        });

        // 4. Cập nhật địa chỉ
        group.MapPut("/{id:int}", async (int id, UpsertAddressRequest req, ClaimsPrincipal p, AddressService addressService) =>
        {
            int ownerId = UserId(p);
            var result = await addressService.UpdateAddressAsync(id, ownerId, req);
            return Results.Ok(ApiResponse<object>.Success(result, "Cập nhật địa chỉ thành công"));
        });

        // 5. Xóa địa chỉ
        group.MapDelete("/{id:int}", async (int id, ClaimsPrincipal p, AddressService addressService) =>
        {
            int ownerId = UserId(p);
            await addressService.DeleteAddressAsync(id, ownerId);
            return Results.Ok(ApiResponse<object>.Success(null, "Xóa địa chỉ thành công"));
        });
    }
}