using System.Diagnostics;

namespace Logistics.Api.Middlewares;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Ghi log response thành công (hoặc hoàn thành request)
            _logger.LogInformation("✅ [HTTP] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                request.Method,
                request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Ghi log lỗi phát sinh tại request này trước khi ném tiếp cho GlobalExceptionMiddleware xử lý
            _logger.LogError(ex, "❌ [HTTP Error] {Method} {Path} failed in {ElapsedMs}ms - Error: {ErrorMessage}",
                request.Method,
                request.Path,
                context.Response.StatusCode == 200 ? 500 : context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw; // Ném tiếp ngoại lệ để GlobalExceptionMiddleware bắt và trả về ApiResponse chuẩn
        }
    }
}