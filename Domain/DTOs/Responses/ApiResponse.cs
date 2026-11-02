using System.Text.Json.Serialization;

namespace Logistics.Api.Domain.DTOs.Responses
{
    /// <summary>
    /// Lớp chuẩn hóa kết quả trả về (API Response) cho toàn bộ hệ thống.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của payload trả về.</typeparam>
    public class ApiResponse<T>
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "ok";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "Thành công";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        // --- Các phương thức static hỗ trợ trả về Response nhanh ---

        /// <summary>
        /// Trả về thành công không kèm Data: { "message": "...", "status": "ok" }
        /// </summary>
        public static ApiResponse<T> Success(string message = "Thành công") =>
            new() { Status = "ok", Message = message, Data = default };

        /// <summary>
        /// Trả về thành công có kèm Data: { "message": "...", "data": {...}, "status": "ok" }
        /// </summary>
        public static ApiResponse<T> Success(T data, string message = "Thành công") =>
            new() { Status = "ok", Message = message, Data = data };

        /// <summary>
        /// Trả về lỗi: { "message": "...", "detail": "..." (nếu có), "status": "error" }
        /// </summary>
        public static ApiResponse<T> Error(string message, string? detail = null) =>
            new() { Status = "error", Message = message, Detail = detail, Data = default };
    }

    /// <summary>
    /// Lớp non-generic tiện ích để gọi nhanh ApiResponse.Success() hoặc ApiResponse.Error() mà không cần truyền kiểu dữ liệu.
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse Success(string message = "Thành công") =>
            new() { Status = "ok", Message = message, Data = null };

        public static ApiResponse Error(string message, string? detail = null) =>
            new() { Status = "error", Message = message, Detail = detail, Data = null };
    }
}
