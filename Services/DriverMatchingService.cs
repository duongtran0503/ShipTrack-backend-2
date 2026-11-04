using Logistics.Api.Data;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions;
using Logistics.Api.Domain.Utils;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services
{
    public class DriverMatchingService
    {
        private readonly AppDbContext _db;
        private const int MaxActiveOrdersPerDriver = 3;

        public DriverMatchingService(AppDbContext db) => _db = db;

        public async Task<List<AvailableOrderResponse>> GetAvailableOrdersAsync(int driverId)
        {
            // 1. Load driver
            var driver = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == driverId)
                ?? throw new AppException(new AuthError(404, "Tài xế không tồn tại."));

            // 2. Online
            if (!driver.Online)
                throw new AppException(new AuthError(400, "Bạn cần bật trạng thái sẵn sàng để xem đơn."));

            // 3. GPS - Nếu thiếu, tự động lấy từ vùng phục vụ mặc định để không bị chặn
            if (driver.Latitude is null || driver.Longitude is null)
            {
                var defaultArea = await _db.DriverServiceAreas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.DriverId == driverId && a.IsDefault && a.IsActive)
                    ?? await _db.DriverServiceAreas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.DriverId == driverId && a.IsActive);

                if (defaultArea == null)
                {
                    throw new AppException(new AuthError(400, "Chưa cấu hình vùng phục vụ và chưa có tọa độ GPS."));
                }

                driver.Latitude = defaultArea.CenterLat;
                driver.Longitude = defaultArea.CenterLng;
            }

            var driverLat = driver.Latitude.Value;
            var driverLng = driver.Longitude.Value;

            // 4. Vehicles
            var driverVehicles = await _db.DriverVehicles.AsNoTracking()
                .Where(v => v.DriverId == driverId && v.IsVerified)
                .Select(v => v.VehicleType)
                .Distinct()
                .ToListAsync();

            if (driverVehicles.Count == 0)
                throw new AppException(new AuthError(400, "Chưa có phương tiện được duyệt."));

            // 5. Service areas
            var serviceAreas = await _db.DriverServiceAreas.AsNoTracking()
                .Where(a => a.DriverId == driverId && a.IsActive)
                .ToListAsync();

            if (serviceAreas.Count == 0)
                throw new AppException(new AuthError(400, "Chưa cấu hình vùng phục vụ."));

            // 6. Active count — có FAILED
            var activeCount = await _db.Orders.AsNoTracking()
                .CountAsync(o => o.DriverId == driverId
                              && o.Status != OrderStatus.COMPLETED
                              && o.Status != OrderStatus.CANCELLED
                              && o.Status != OrderStatus.FAILED);

            if (activeCount >= MaxActiveOrdersPerDriver)
                return new List<AvailableOrderResponse>();

            // 7. Bounding box
            var maxRadius = serviceAreas.Max(a => a.RadiusKm);
            var latDelta = maxRadius / 111.0;
            var lngDelta = maxRadius / (111.0 * Math.Cos(driverLat * Math.PI / 180.0));

            var minLat = driverLat - latDelta;
            var maxLat = driverLat + latDelta;
            var minLng = driverLng - lngDelta;
            var maxLng = driverLng + lngDelta;

            // 8. Query SQL (Loại bỏ các đơn thiếu tọa độ GPS bằng cách check HasValue)
            var candidates = await _db.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.SEARCHING_DRIVER
                         && o.DriverId == null
                         && o.PickupLat != null && o.PickupLng != null
                         && o.DropoffLat != null && o.DropoffLng != null
                         && driverVehicles.Contains(o.VehicleType)   
                         && o.PickupLat >= minLat && o.PickupLat <= maxLat
                         && o.PickupLng >= minLng && o.PickupLng <= maxLng)
                .OrderByDescending(o => o.CreatedAtUtc)
                .Take(100)
                .ToListAsync();

            // 9. Filter Haversine (Sử dụng .Value vì chắc chắn không null nhờ bước 8)
            var available = candidates
                .Where(o =>
                {
                    double pLat = o.PickupLat!.Value;
                    double pLng = o.PickupLng!.Value;
                    double dLat = o.DropoffLat!.Value;
                    double dLng = o.DropoffLng!.Value;

                    var pickupDistance = GeoUtils.Haversine(driverLat, driverLng, pLat, pLng);
                    if (pickupDistance > maxRadius) return false;

                    var tripDistance = GeoUtils.Haversine(pLat, pLng, dLat, dLng);
                    if (tripDistance > maxRadius) return false;

                    return serviceAreas.Any(area =>
                        GeoUtils.Haversine(area.CenterLat, area.CenterLng, pLat, pLng) <= area.RadiusKm
                        && GeoUtils.Haversine(area.CenterLat, area.CenterLng, dLat, dLng) <= area.RadiusKm);
                })
                .OrderBy(o => GeoUtils.Haversine(driverLat, driverLng, o.PickupLat!.Value, o.PickupLng!.Value))
                .Take(50)
                .Select(o => new AvailableOrderResponse
                {
                    Id = o.Id,
                    TrackingCode = o.TrackingCode,
                    PickupAddress = o.PickupAddress,
                    DropoffAddress = o.DropoffAddress,
                    PickupLat = o.PickupLat!.Value,
                    PickupLng = o.PickupLng!.Value,
                    DropoffLat = o.DropoffLat!.Value,
                    DropoffLng = o.DropoffLng!.Value,
                    DistanceFromDriverKm = Math.Round(
                        GeoUtils.Haversine(driverLat, driverLng, o.PickupLat!.Value, o.PickupLng!.Value), 1),
                    TripDistanceKm = Math.Round(
                        GeoUtils.Haversine(o.PickupLat!.Value, o.PickupLng!.Value, o.DropoffLat!.Value, o.DropoffLng!.Value), 1),
                    ShippingFee = o.ShippingFee,
                    ServiceType = o.ServiceType,
                    VehicleType = o.VehicleType.ToString(), // Chuyển Enum sang string cho DTO
                    CreatedAtUtc = o.CreatedAtUtc,
                })
                .ToList();

            return available;
        }


        /// <summary>
        /// Xem chi tiết đơn hàng đang chờ tài xế nhận.
        /// </summary>
        public async Task<OrderDetailResponse> GetOrderDetailAsync(int driverId, int orderId)
        {
            var order = await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng."));

            if (order.Status != OrderStatus.SEARCHING_DRIVER || order.DriverId != null)
            {
                throw new AppException(new AuthError(400, "Đơn hàng này không còn ở trạng thái tìm tài xế hoặc đã có tài xế nhận."));
            }

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

        /// <summary>
        /// Tài xế bấm nhận đơn hàng.
        /// </summary>
        public async Task<OrderDetailResponse> AcceptOrderAsync(int driverId, int orderId)
        {
            // 1. Kiểm tra tài xế và trạng thái sẵn sàng
            var driver = await _db.Users.FirstOrDefaultAsync(u => u.Id == driverId)
                ?? throw new AppException(new AuthError(404, "Tài xế không tồn tại."));

            if (!driver.Online)
                throw new AppException(new AuthError(400, "Bạn cần bật trạng thái trực tuyến để nhận đơn."));

            // 2. Kiểm tra phương tiện đã được duyệt của tài xế
            var driverVehicles = await _db.DriverVehicles
                .Where(v => v.DriverId == driverId && v.IsVerified)
                .Select(v => v.VehicleType)
                .ToListAsync();

            if (driverVehicles.Count == 0)
                throw new AppException(new AuthError(400, "Tài khoản chưa có phương tiện được duyệt."));

            // 3. Lấy đơn hàng cần nhận
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng."));

            // 🔥 XỬ LÝ IDEMPOTENCY: Nếu đơn hàng đã thuộc về chính tài xế này rồi -> Cho qua luôn không báo lỗi
            if (order.DriverId == driverId)
            {
                return await GetActiveOrderDetailAsync(driverId, orderId);
            }

            // 4. Kiểm tra số lượng đơn đang thực hiện (tối đa 3 đơn)
            var activeCount = await _db.Orders
                .CountAsync(o => o.DriverId == driverId
                              && o.Status != OrderStatus.COMPLETED
                              && o.Status != OrderStatus.CANCELLED
                              && o.Status != OrderStatus.FAILED);

            if (activeCount >= MaxActiveOrdersPerDriver)
                throw new AppException(new AuthError(400, $"Bạn đã đạt giới hạn tối đa {MaxActiveOrdersPerDriver} đơn hàng đang thực hiện cùng lúc."));

            // 5. Kiểm tra trạng thái đơn có đang tìm tài xế không
            if (order.Status != OrderStatus.SEARCHING_DRIVER || order.DriverId != null)
            {
                throw new AppException(new AuthError(400, "Rất tiếc, đơn hàng này đã có tài xế khác nhận hoặc đã thay đổi trạng thái."));
            }

            // 6. Kiểm tra loại phương tiện có khớp không
            if (!driverVehicles.Contains(order.VehicleType))
            {
                throw new AppException(new AuthError(400, "Phương tiện của bạn không phù hợp với loại yêu cầu của đơn hàng này."));
            }

            // 7. Cập nhật gán đơn cho tài xế
            order.DriverId = driverId;
            order.Status = OrderStatus.DRIVER_ASSIGNED;
            order.UpdatedAtUtc = DateTime.UtcNow;
            order.AcceptedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Trả về chi tiết đơn hàng sau khi nhận thành công
            return await GetActiveOrderDetailAsync(driverId, orderId);
        }

        /// <summary>
        /// Lấy danh sách đơn hàng tài xế đã nhận và đang thực hiện (chưa hoàn thành/hủy).
        /// </summary>
        public async Task<List<AvailableOrderResponse>> GetActiveOrdersForDriverAsync(int driverId)
        {
            var orders = await _db.Orders.AsNoTracking()
                .Where(o => o.DriverId == driverId
                       && o.Status != OrderStatus.COMPLETED
                       && o.Status != OrderStatus.CANCELLED
                       && o.Status != OrderStatus.FAILED)
                .OrderByDescending(o => o.UpdatedAtUtc)
                .ToListAsync();

            // Có thể dùng lại AvailableOrderResponse hoặc mapping sang DTO tương ứng
            return orders.Select(o => new AvailableOrderResponse
            {
                Id = o.Id,
                TrackingCode = o.TrackingCode,
                PickupAddress = o.PickupAddress,
                DropoffAddress = o.DropoffAddress,
                PickupLat = o.PickupLat ?? 0,
                PickupLng = o.PickupLng ?? 0,
                DropoffLat = o.DropoffLat ?? 0,
                DropoffLng = o.DropoffLng ?? 0,
                DistanceFromDriverKm = 0, // Đang thực hiện nên không cần tính khoảng cách từ driver tới điểm lấy nữa
                TripDistanceKm = o.DistanceKm,
                ShippingFee = o.ShippingFee,
                ServiceType = o.ServiceType,
                VehicleType = o.VehicleType.ToString(),
                CreatedAtUtc = o.CreatedAtUtc
            }).ToList();
        }

        /// <summary>
        /// Lấy lịch sử đơn hàng đã hoàn thành hoặc đã hủy của tài xế (có phân trang).
        /// </summary>
        public async Task<(List<OrderDetailResponse> Items, int TotalCount)> GetDriverOrderHistoryAsync(int driverId, int pageNumber, int pageSize)
        {
            var query = _db.Orders.AsNoTracking()
                .Where(o => o.DriverId == driverId
                       && (o.Status == OrderStatus.COMPLETED
                           || o.Status == OrderStatus.CANCELLED
                           || o.Status == OrderStatus.FAILED));

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.UpdatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = orders.Select(o => new OrderDetailResponse
            {
                Id = o.Id,
                TrackingCode = o.TrackingCode,
                PickupAddress = o.PickupAddress,
                DropoffAddress = o.DropoffAddress,
                PickupLat = o.PickupLat,
                PickupLng = o.PickupLng,
                DropoffLat = o.DropoffLat,
                DropoffLng = o.DropoffLng,
                DistanceKm = o.DistanceKm,
                SenderName = o.SenderName,
                SenderPhone = o.SenderPhone,
                ReceiverName = o.ReceiverName,
                ReceiverPhone = o.ReceiverPhone,
                PackageName = o.PackageName,
                WeightKg = o.WeightKg,
                PackageDetails = o.PackageDetails,
                Note = o.Note,
                PaymentMethod = o.PaymentMethod.ToString(),
                IsBulky = o.IsBulky,
                IsDoorDelivery = o.IsDoorDelivery,
                TipAmount = o.TipAmount,
                CodAmount = o.CodAmount,
                ShippingFee = o.ShippingFee,
                TotalPrice = o.TotalPrice,
                ServiceType = o.ServiceType,
                VehicleType = o.VehicleType.ToString(),
                Status = o.Status.ToString(),
                CreatedAtUtc = o.CreatedAtUtc
            }).ToList();

            return (items, totalCount);
        }

        /// <summary>
        /// Xem chi tiết đơn hàng đã nhận/đang thực hiện của tài xế.
        /// </summary>
        public async Task<OrderDetailResponse> GetActiveOrderDetailAsync(int driverId, int orderId)
        {
            var order = await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new AppException(new AuthError(404, "Không tìm thấy đơn hàng."));

            // Kiểm tra xem đơn hàng này có thuộc về tài xế đang gọi API không
            if (order.DriverId != driverId)
            {
                throw new AppException(new AuthError(403, "Bạn không có quyền xem chi tiết đơn hàng này vì không được phân công vận chuyển."));
            }

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