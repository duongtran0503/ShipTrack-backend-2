namespace Logistics.Api.Domain.Constants;

/// <summary>
/// Các loại phương tiện vận chuyển hỗ trợ trong hệ thống.
/// </summary>
public enum VehicleType
{
    MOTORBIKE, // Xe máy giao nhanh
    VAN,       // Xe tải nhỏ / Van
    TRUCK,     // Xe tải lớn
    CONTAINER  // Xe container
}