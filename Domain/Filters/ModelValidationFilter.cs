using Logistics.Api.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
            if (arg is null) continue;

            var type = arg.GetType();

            // Bỏ qua type hệ thống / service / primitive
            if (type.IsPrimitive
                || type.IsEnum
                || type.IsValueType
                || type == typeof(string)
                || type == typeof(ClaimsPrincipal)
                || type.Namespace?.StartsWith("System") == true
                || type.Namespace?.StartsWith("Microsoft") == true
                || type.Namespace?.Contains(".Services") == true)
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