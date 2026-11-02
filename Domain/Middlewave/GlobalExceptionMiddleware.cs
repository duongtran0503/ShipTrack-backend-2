using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Exceptions; 
using System.Net;
using System.Text.Json;

namespace Logistics.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Chỉ ghi đè 401/403 do mặc định của ASP.NET Core sinh ra (khi chưa có Body)
            if (!context.Response.HasStarted
                && (context.Response.StatusCode == 401 || context.Response.StatusCode == 403))
            {
                _logger.LogWarning("Intercepting {StatusCode} for {Path}",
                    context.Response.StatusCode, context.Request.Path);

                await RewriteAuthResponseAsync(context);
            }
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Business exception occurred: {Message}", ex.Message);
            await HandleAppExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private static Task HandleAppExceptionAsync(HttpContext context, AppException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = ex.ErrorCode.StatusCode;

        // Sử dụng chuẩn ApiResponse.Error của dự án
        var response = ApiResponse.Error(ex.Message, detail: null);

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }

    private static Task HandleUnhandledExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = ApiResponse.Error("Lỗi hệ thống nội bộ.", detail: ex.Message);

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }

    private async Task RewriteAuthResponseAsync(HttpContext context)
    {
        try
        {
            var message = context.Response.StatusCode switch
            {
                401 => "Người dùng chưa đăng nhập hoặc token không hợp lệ",
                403 => "Bạn không có quyền truy cập tài nguyên này",
                _ => "Lỗi xác thực hệ thống"
            };

            var response = ApiResponse.Error(message);

            context.Response.ContentType = "application/json";

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rewrite auth response");
        }
    }
}