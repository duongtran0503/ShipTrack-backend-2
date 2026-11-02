namespace Logistics.Api.Domain.Constants;

/// <summary>
/// Các phương thức thanh toán hỗ trợ trong hệ thống.
/// </summary>
public enum PaymentMethod
{
    CASH, // Tiền mặt (COD)
    BANK  // Chuyển khoản ngân hàng / Ví điện tử
}