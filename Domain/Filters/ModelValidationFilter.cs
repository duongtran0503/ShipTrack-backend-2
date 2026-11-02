using System.ComponentModel.DataAnnotations;
using Logistics.Api.Domain.Exceptions;

namespace Logistics.Api.Common.Filters;

public class ModelValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Duyệt qua tất cả các tham số truyền vào Endpoint
        Console.WriteLine($"[FILTER] Called with {context.Arguments.Count} args");
        foreach (var arg in context.Arguments)
        {
            Console.WriteLine($"[FILTER] arg type: {arg?.GetType().FullName ?? "null"}");
            if (arg == null)
            {
                // Ném AppException thay vì trả về Result để GlobalExceptionMiddleware xử lý đồng bộ
                throw new AppException(new AuthError(400, "Dữ liệu đầu vào không được để trống hoặc null."));
            }

            var type = arg.GetType();

            // Bỏ qua các kiểu nguyên thủy, chuỗi, hoặc các đối tượng hệ thống (HttpContext, ClaimsPrincipal, Services...)
            if (type.IsPrimitive || type == typeof(string) || type.Namespace?.StartsWith("Microsoft") == true)
                continue;

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(arg);

            // Tiến hành validate Data Annotations ([Required], [Range], [Phone], v.v.)
            if (!Validator.TryValidateObject(arg, validationContext, validationResults, true))
            {
                var errorMsg = validationResults.FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";

                // Ném AppException kèm thông điệp lỗi, middleware sẽ tự động bọc vào ApiResponse
                throw new AppException(new AuthError(400, errorMsg));
            }
        }

        // Nếu hợp lệ, tiếp tục chuyển request vào Endpoint
        return await next(context);
    }
}