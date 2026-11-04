using Logistics.Api.Data;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Exceptions;
using Logistics.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services
{
    public class DriverService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<LogisticsHub> _hub;

        public DriverService(AppDbContext db, IHubContext<LogisticsHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        /// <summary>
        /// Bật/tắt trạng thái sẵn sàng nhận đơn của tài xế.
        /// </summary>
        public async Task<DriverStatusResponse> UpdateStatusAsync(int driverId, bool isOnline)
        {
            var driver = await _db.Users.FindAsync(driverId)
                ?? throw new AppException(new AuthError(404, "Tài xế không tồn tại."));

            // Chỉ driver mới được bật/tắt
            if (!driver.Role.StartsWith("DRIVER", StringComparison.OrdinalIgnoreCase))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có thể đổi trạng thái."));

            // Nếu driver đang bị khóa → không cho online
            if (!driver.IsActive && isOnline)
                throw new AppException(new AuthError(400, "Tài khoản đang bị khóa, không thể bật sẵn sàng."));

            // Nếu không đổi gì → trả luôn (idempotent)
            if (driver.Online == isOnline)
                return DriverStatusResponse.FromEntity(driver);

            driver.Online = isOnline;

            // Nếu chuyển sang online, tự động lấy tọa độ từ vùng phục vụ mặc định (IsDefault = true) để test nhanh
            if (isOnline)
            {
                var defaultArea = await _db.DriverServiceAreas
                    .FirstOrDefaultAsync(a => a.DriverId == driverId && a.IsDefault && a.IsActive);

                if (defaultArea == null)
                {
                    // Fallback lấy vùng bất kỳ đang active nếu chưa set default
                    defaultArea = await _db.DriverServiceAreas
                        .FirstOrDefaultAsync(a => a.DriverId == driverId && a.IsActive);
                }

                if (defaultArea != null)
                {
                    driver.Latitude = defaultArea.CenterLat;
                    driver.Longitude = defaultArea.CenterLng;
                }
                else
                {
                    throw new AppException(new AuthError(400, "Tài xế chưa cài đặt hoặc kích hoạt khu vực phục vụ nào."));
                }
            }

            await _db.SaveChangesAsync();

            var payload = DriverStatusResponse.FromEntity(driver);

            // 🔥 Broadcast real-time tới tất cả group liên quan
            await _hub.Clients.Group("role_admin")
                .SendAsync("DriverStatusChanged", payload);

            await _hub.Clients.Group($"role_{driver.Role.ToLowerInvariant()}")
                .SendAsync("DriverStatusChanged", payload);

            // Gửi riêng cho chính driver (sync tab khác)
            await _hub.Clients.Group($"driver_{driverId}")
                .SendAsync("DriverStatusChanged", payload);

            // Gửi tới customer đang có đơn của driver này (nếu có)
            var customerIds = await _db.Orders
                .AsNoTracking()
                .Where(o => o.DriverId == driverId
                       && o.Status != OrderStatus.COMPLETED
                       && o.Status != OrderStatus.CANCELLED)
                .Select(o => o.CustomerId)
                .Distinct()
                .ToListAsync();

            foreach (var cid in customerIds)
            {
                await _hub.Clients.Group($"customer_{cid}")
                    .SendAsync("DriverStatusChanged", payload);
            }

            return payload;
        }
        /// <summary>
        /// Lấy thống kê dashboard cho driver (real data).
        /// </summary>
        public async Task<object> GetDashboardStatsAsync(int driverId)
        {
            var driver = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == driverId)
                ?? throw new AppException(new AuthError(404, "Tài xế không tồn tại."));

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Đơn đang hoạt động (chưa xong, chưa hủy)
            var activeCount = await _db.Orders.AsNoTracking()
                .CountAsync(o => o.DriverId == driverId
                              && o.Status != OrderStatus.COMPLETED
                              && o.Status != OrderStatus.CANCELLED);

            // Đơn hoàn thành hôm nay
            var completedToday = await _db.Orders.AsNoTracking()
                .CountAsync(o => o.DriverId == driverId
                              && o.Status == OrderStatus.COMPLETED
                              && o.CreatedAtUtc >= today
                              && o.CreatedAtUtc < tomorrow);

            // Thu nhập hôm nay
            var todayEarnings = await _db.Orders.AsNoTracking()
        .Where(o => o.DriverId == driverId
                 && o.Status == OrderStatus.COMPLETED
                 && o.CreatedAtUtc >= today
                 && o.CreatedAtUtc < tomorrow)
        .SumAsync(o => (long)o.ShippingFee);  

    
            decimal todayEarningsDecimal = todayEarnings;

            // Rating trung bình (nếu có bảng Review)
            var avgRating = 4.8;

            return new
            {
                activeOrdersCount = activeCount,
                completedTodayCount = completedToday,
                todayEarnings = todayEarningsDecimal,
                averageRating = Math.Round(avgRating, 1),
                isOnline = driver.Online,
            };
        }


        /// <summary>
        /// Cập nhật tọa độ GPS trực tiếp cho tài xế và broadcast real-time qua SignalR.
        /// </summary>
        public async Task UpdateDriverLocationAsync(int driverId, double latitude, double longitude)
        {
            var driver = await _db.Users.FindAsync(driverId)
                ?? throw new AppException(new AuthError(404, "Tài xế không tồn tại."));

            if (!driver.Role.StartsWith("DRIVER", StringComparison.OrdinalIgnoreCase))
                throw new AppException(new AuthError(403, "Chỉ tài xế mới có thể cập nhật vị trí."));

            // Cập nhật tọa độ mới vào database
            driver.Latitude = latitude;
            driver.Longitude = longitude;
            await _db.SaveChangesAsync();

            // Chuẩn bị payload thông tin vị trí realtime
            var payload = new
            {
                driverId = driver.Id,
                driverName = driver.Name,
                latitude = driver.Latitude,
                longitude = driver.Longitude,
                updatedAtUtc = DateTime.UtcNow
            };

            // 🔥 BROADCAST REAL-TIME QUA SIGNALR

            // 1. Gửi tới toàn bộ Admin
            await _hub.Clients.Group("role_admin")
                .SendAsync("DriverLocationChanged", payload);

            // 2. Gửi tới chính tài xế (đồng bộ các tab)
            await _hub.Clients.Group($"driver_{driverId}")
                .SendAsync("DriverLocationChanged", payload);

            // 3. Gửi tới tất cả các Customer đang có đơn hàng active với tài xế này
            var activeOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.DriverId == driverId
                       && o.Status != OrderStatus.COMPLETED
                       && o.Status != OrderStatus.CANCELLED
                       && o.Status != OrderStatus.FAILED)
                .Select(o => new { o.Id, o.CustomerId })
                .Distinct()
                .ToListAsync();

            foreach (var order in activeOrders)
            {
                // Gửi qua group riêng của đơn hàng (hỗ trợ tracking trực tiếp trang chi tiết đơn)
                await _hub.Clients.Group($"order_{order.Id}")
                    .SendAsync("DriverLocationChanged", payload);

                // Gửi qua group của customer tương ứng
                await _hub.Clients.Group($"customer_{order.CustomerId}")
                    .SendAsync("DriverLocationChanged", payload);
            }
        }
    }
}
