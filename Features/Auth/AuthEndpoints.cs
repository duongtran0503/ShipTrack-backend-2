using Logistics.Api.Common.Filters; // Chứa ModelValidationFilter
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Services;

namespace Logistics.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").AddEndpointFilter<ModelValidationFilter>();

        group.MapPost("/register", async (RegisterRequest req, AuthService authService) =>
        {
            // Validate tự động bằng 1 dòng, sai là ném AppException ngay lập tức
           

            var result = await authService.RegisterAsync(req);
            return Results.Created("/api/users/me", ApiResponse<object>.Success(result, "Đăng ký thành công"));
        });

        group.MapPost("/login", async (LoginRequest req, AuthService authService) =>
        {
            try
            {
                // Validate tự động bằng 1 dòng, tuyệt đối không bao giờ lọt xuống Service nếu thiếu/sai email, pass

                var result = await authService.LoginAsync(req);
                return Results.Ok(ApiResponse<object>.Success(result, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        });
    }
}