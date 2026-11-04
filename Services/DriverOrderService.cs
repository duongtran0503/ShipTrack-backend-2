using Logistics.Api.Data;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions;
using Logistics.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services
{
    public class DriverOrderService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<LogisticsHub> _hub;
        private const int MaxActiveOrdersPerDriver = 3;

        public DriverOrderService(AppDbContext db, IHubContext<LogisticsHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // ==================== 1. NHẬN ĐƠN HÀNG (ACCEPT ORDER) ====================
            //Đã sử lý bên drivermarchingservice rồi

        // ==================== 2. ĐÃ LẤY HÀNG (PICKED_UP) ====================
        public async Task<OrderDetailResponse> MarkAsPickedUpAsync(int driverId, int orderId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng."));

            if (order.DriverId != driverId)
                throw new AppException(new AuthError(403, "Bạn không có quyền thao tác trên đơn hàng này."));

            if (order.Status != OrderStatus.DRIVER_ASSIGNED)
                throw new AppException(new AuthError(400, "Đơn hàng phải ở trạng thái đã nhận (DRIVER_ASSIGNED) mới có thể xác nhận lấy hàng."));

            order.Status = OrderStatus.PICKED_UP;
            order.UpdatedAtUtc = DateTime.UtcNow;
            order.PickedUpAtUtc = DateTime.UtcNow;

            //order.History.Add(new OrderStatusHistory
            //{
            //    Status = OrderStatus.PICKED_UP,
            //    Note = $"Tài xế #{driverId} đã lấy hàng thành công.",
            //    CreatedAtUtc = DateTime.UtcNow
            //});

            await _db.SaveChangesAsync();
            var orderDetail = await GetOrderDetailAsync(driverId, orderId);

            // 🔥 BROADCAST REAL-TIME QUA SIGNALR
            await BroadcastOrderStatusChangedAsync(order.Id, order.CustomerId, driverId, orderDetail);

            return orderDetail;
        }

        // ==================== 3. ĐANG GIAO HÀNG (DELIVERING) ====================
        public async Task<OrderDetailResponse> MarkAsDeliveringAsync(int driverId, int orderId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng."));

            if (order.DriverId != driverId)
                throw new AppException(new AuthError(403, "Bạn không có quyền thao tác trên đơn hàng này."));

            if (order.Status != OrderStatus.PICKED_UP)
                throw new AppException(new AuthError(400, "Đơn hàng phải ở trạng thái đã lấy hàng (PICKED_UP) mới chuyển sang đang giao."));

            order.Status = OrderStatus.DELIVERING;
            order.UpdatedAtUtc = DateTime.UtcNow;
            

            //order.History.Add(new OrderStatusHistory
            //{
            //    Status = OrderStatus.DELIVERING,
            //    Note = $"Tài xế #{driverId} đang trên đường giao hàng.",
            //    CreatedAtUtc = DateTime.UtcNow
            //});

            await _db.SaveChangesAsync();
            var orderDetail = await GetOrderDetailAsync(driverId, orderId);

            // 🔥 BROADCAST REAL-TIME QUA SIGNALR
            await BroadcastOrderStatusChangedAsync(order.Id, order.CustomerId, driverId, orderDetail);

            return orderDetail;
        }

        // ==================== 4. HOÀN THÀNH ĐƠN HÀNG (COMPLETED) ====================
        public async Task<OrderDetailResponse> MarkAsCompletedAsync(int driverId, int orderId, string? proofPhotoPath)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng."));

            if (order.DriverId != driverId)
                throw new AppException(new AuthError(403, "Bạn không có quyền thao tác trên đơn hàng này."));

            if (order.Status != OrderStatus.DELIVERING)
                throw new AppException(new AuthError(400, "Đơn hàng phải ở trạng thái đang giao (DELIVERING) mới có thể hoàn thành."));

            order.Status = OrderStatus.COMPLETED;
            order.CompletedAtUtc = DateTime.UtcNow;
            order.UpdatedAtUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(proofPhotoPath))
            {
                order.ProofPhotoPath = proofPhotoPath.Trim();
            }

            var driver = await _db.Users.FirstOrDefaultAsync(u => u.Id == driverId);
            if (driver != null)
            {
                driver.Balance += (order.ShippingFee + order.TipAmount);
            }

            //order.History.Add(new OrderStatusHistory
            //{
            //    Status = OrderStatus.COMPLETED,
            //    Note = $"Đơn hàng đã hoàn thành thành công bởi tài xế #{driverId}.",
            //    CreatedAtUtc = DateTime.UtcNow
            //});

            await _db.SaveChangesAsync();
            var orderDetail = await GetOrderDetailAsync(driverId, orderId);

            // 🔥 BROADCAST REAL-TIME QUA SIGNALR
            await BroadcastOrderStatusChangedAsync(order.Id, order.CustomerId, driverId, orderDetail);

            return orderDetail;
        }

        // ==================== 5. ĐƠN HÀNG THẤT BẠI / HỦY (FAILED) ====================
        public async Task<OrderDetailResponse> MarkAsFailedAsync(int driverId, int orderId, string? failureReason)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng."));

            if (order.DriverId != driverId)
                throw new AppException(new AuthError(403, "Bạn không có quyền thao tác trên đơn hàng này."));

            // Đơn hàng phải đang ở trạng thái có thể báo thất bại (ví dụ: đã nhận, đã lấy hoặc đang giao)
            if (order.Status == OrderStatus.COMPLETED || order.Status == OrderStatus.CANCELLED || order.Status == OrderStatus.FAILED)
            {
                throw new AppException(new AuthError(400, "Đơn hàng đã kết thúc, không thể cập nhật trạng thái thất bại."));
            }

            order.Status = OrderStatus.FAILED;
            order.FailedAtUtc = DateTime.UtcNow;
            order.UpdatedAtUtc = DateTime.UtcNow;
            order.FailureReason = !string.IsNullOrWhiteSpace(failureReason) ? failureReason.Trim() : "Tài xế không thể giao hàng.";

            await _db.SaveChangesAsync();
            var orderDetail = await GetOrderDetailAsync(driverId, orderId);

            // 🔥 BROADCAST REAL-TIME QUA SIGNALR
            await BroadcastOrderStatusChangedAsync(order.Id, order.CustomerId, driverId, orderDetail);

            return orderDetail;
        }
        // ==================== HELPER BROADCAST SIGNALR ====================
        private async Task BroadcastOrderStatusChangedAsync(int orderId, int customerId, int driverId, OrderDetailResponse payload)
        {
            // 1. Gửi tới group riêng của đơn hàng (cả Customer, Driver và Admin join group này sẽ nhận được)
            await _hub.Clients.Group($"order_{orderId}")
                .SendAsync("OrderStatusChanged", payload);

            // 2. Gửi tới Customer cụ thể đang theo dõi
            await _hub.Clients.Group($"customer_{customerId}")
                .SendAsync("OrderStatusChanged", payload);

            // 3. Gửi tới Driver cụ thể (sync các tab của tài xế)
            await _hub.Clients.Group($"driver_{driverId}")
                .SendAsync("OrderStatusChanged", payload);

            // 4. Gửi tới Admin quản lý hệ thống
            await _hub.Clients.Group("role_admin")
                .SendAsync("OrderStatusChanged", payload);
        }



        public async Task<OrderDetailResponse> GetOrderDetailAsync(int driverId, int orderId)
        {
            var order = await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng."));

            return new OrderDetailResponse
            {
                Id = order.Id,
                TrackingCode = order.TrackingCode,
                PickupAddress = order.PickupAddress,
                DropoffAddress = order.DropoffAddress,
                PickupLat = order.PickupLat,
                PickupLng = order.PickupLng,
                DropoffLat = order.DropoffLat,
                DropoffLng = order.DropoffLng,
                DistanceKm = order.DistanceKm,
                SenderName = order.SenderName,
                SenderPhone = order.SenderPhone,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                PackageName = order.PackageName,
                WeightKg = order.WeightKg,
                PackageDetails = order.PackageDetails,
                Note = order.Note,
                PaymentMethod = order.PaymentMethod.ToString(),
                IsBulky = order.IsBulky,
                IsDoorDelivery = order.IsDoorDelivery,
                TipAmount = order.TipAmount,
                CodAmount = order.CodAmount,
                ShippingFee = order.ShippingFee,
                TotalPrice = order.TotalPrice,
                ServiceType = order.ServiceType,
                VehicleType = order.VehicleType.ToString(),
                Status = order.Status.ToString(),
                CreatedAtUtc = order.CreatedAtUtc
            };
        }

    }
}
