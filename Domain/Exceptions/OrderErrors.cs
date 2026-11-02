using Logistics.Api.Domain.Interfaces;

namespace Logistics.Api.Domain.Exceptions;

public enum OrderErrorCodes : int
{
    InvalidGps = 4001,
    MissingOrderInfo = 4002,
    UnauthorizedCustomer = 4031
}

public class OrderError : IErrorCode
{
    public int StatusCode { get; }
    public string Message { get; }
    public bool Success => false;

    public OrderError(int statusCode, string message)
    {
        StatusCode = statusCode;
        Message = message;
    }

    // Các Error định nghĩa sẵn
    public static readonly IErrorCode InvalidGps = new OrderError(400, "Vị trí GPS không hợp lệ.");
    public static readonly IErrorCode MissingOrderInfo = new OrderError(400, "Thiếu thông tin lấy/giao hàng bắt buộc.");
    public static readonly IErrorCode ForbiddenCustomer = new OrderError(403, "Chỉ có khách hàng mới có quyền tạo đơn.");
}