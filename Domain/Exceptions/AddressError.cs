using Logistics.Api.Domain.Interfaces;

namespace Logistics.Api.Domain.Exceptions
{
    public static class AddressError
    {
        public static readonly IErrorCode NotFound = new OrderError(404, "Không tìm thấy địa chỉ.");
        public static readonly IErrorCode Forbidden = new OrderError(403, "Bạn không có quyền thao tác trên địa chỉ này.");
        public static readonly IErrorCode InvalidGps = new OrderError(400, "Tọa độ GPS không hợp lệ.");
        public static readonly IErrorCode DuplicatePhone = new OrderError(400, "Số điện thoại này đã tồn tại trong sổ địa chỉ của bạn.");
    }
}
