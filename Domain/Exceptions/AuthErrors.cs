using Logistics.Api.Domain.Interfaces;

namespace Logistics.Api.Domain.Exceptions;

/// <summary>
/// Định nghĩa các mã lỗi (Error Codes) liên quan đến quá trình Xác thực và Đăng ký/Đăng nhập.
/// </summary>
public class AuthError : IErrorCode
{
    public int StatusCode { get; }
    public string Message { get; }
    public bool Success => false;

    public AuthError(int statusCode, string message)
    {
        StatusCode = statusCode;
        Message = message;
    }

    // --- Các ngoại lệ xác thực chuẩn của hệ thống ---
    public static readonly IErrorCode InvalidRole = new AuthError(400, "Vai trò người dùng không hợp lệ.");
    public static readonly IErrorCode DriverLicenseRequired = new AuthError(400, "Tài khoản tài xế bắt buộc phải khai báo biển số xe.");
    public static readonly IErrorCode EmailAlreadyExists = new AuthError(409, "Địa chỉ email này đã tồn tại trong hệ thống.");
    public static readonly IErrorCode InvalidCredentials = new AuthError(401, "Email hoặc mật khẩu không chính xác.");

    public static IErrorCode AccountLocked(string? reason) =>
        new AuthError(403, $"Tài khoản của bạn đã bị khóa. Lý do: {reason ?? "Không rõ"}.");
}