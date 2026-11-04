using Logistics.Api.Data;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Mappings;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions; // Chứa AppException và OrderError
using Logistics.Api.Hubs;
using Logistics.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api.Services;

/// <summary>
/// Lớp dịch vụ xử lý logic tạo đơn hàng và tích hợp Real-time qua SignalR.
/// </summary>
public class OrderService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<LogisticsHub> _hub;

    public OrderService(AppDbContext db, IHubContext<LogisticsHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    /// <summary>
    /// Tạo mới một đơn hàng từ phía Khách hàng, kiểm tra tính hợp lệ dữ liệu và ném AppException nếu vi phạm.
    /// </summary>
    public async Task<DeliveryOrder> CreateOrderAsync(int customerId, CreateOrderRequest req)
    {
        // 1. Validate GPS trực tiếp trong Service và ném AppException
        if (!ValidGps(req.PickupLat, req.PickupLng) || !ValidGps(req.DropoffLat, req.DropoffLng))
        {
            throw new AppException(OrderError.InvalidGps);
        }

        // 2. Validate thông tin cơ bản
        if (string.IsNullOrWhiteSpace(req.PickupAddress) || string.IsNullOrWhiteSpace(req.DropoffAddress) ||
            string.IsNullOrWhiteSpace(req.ReceiverName) || string.IsNullOrWhiteSpace(req.ReceiverPhone))
        {
            throw new AppException(OrderError.MissingOrderInfo);
        }

        var (baseP, total, distance) = Quote(req);
        var discount = Math.Max(0, baseP + (req.IsBulky ? 20000 : 0) + (req.IsDoorDelivery ? 10000 : 0) + req.TipAmount - total);

        var trackingCode = $"TRK-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        var o = new DeliveryOrder
        {
            TrackingCode = trackingCode,
            CustomerId = customerId,
            PickupAddress = req.PickupAddress.Trim(),
            DropoffAddress = req.DropoffAddress.Trim(),
            PickupLat = req.PickupLat,
            PickupLng = req.PickupLng,
            DropoffLat = req.DropoffLat,
            DropoffLng = req.DropoffLng,
            DistanceKm = distance,
            VehicleType = Enum.TryParse<VehicleType>(req.VehicleType, true, out var vt) ? vt : VehicleType.MOTORBIKE,
            ServiceType = req.ServiceType,
            SenderName = req.SenderName,
            SenderPhone = req.SenderPhone,
            ReceiverName = req.ReceiverName,
            ReceiverPhone = req.ReceiverPhone,
            PackageName = req.PackageName ?? "Hàng hóa thông thường",
            WeightKg = req.WeightKg,
            PackageDetails = req.PackageDetails ?? "",
            Note = req.Note,
            PaymentMethod = Enum.TryParse<PaymentMethod>(req.PaymentMethod, true, out var pm) ? pm : PaymentMethod.CASH,
            IsBulky = req.IsBulky,
            IsDoorDelivery = req.IsDoorDelivery,
            TipAmount = req.TipAmount,
            CodAmount = req.CodAmount,
            BasePrice = baseP,
            ShippingFee = total - req.TipAmount,
            TotalPrice = total,
            DiscountAmount = discount,
            PromotionCode = req.PromotionCode,
            Status = OrderStatus.SEARCHING_DRIVER
        };

        _db.Orders.Add(o);
        await _db.SaveChangesAsync();

        _db.StatusHistory.Add(new OrderStatusHistory { OrderId = o.Id, ActorId = customerId, Status = o.Status.ToString() });
        await _db.SaveChangesAsync();

        o.Customer = await _db.Users.FindAsync(o.CustomerId) ?? null!;

        var payload = o.ToResponse();

        await _hub.Clients.Group("role_admin").SendAsync("OrderCreated", payload);
        var driverRole = req.ServiceType switch { "truck" => UserRole.DriverTruck, "container" => UserRole.DriverContainer, _ => UserRole.DriverExpress };
        await _hub.Clients.Group($"role_{driverRole.ToLowerInvariant()}").SendAsync("OrderCreated", payload);
        await _hub.Clients.Group($"role_{driverRole.ToLowerInvariant()}").SendAsync("RealtimeEvent", new { eventName = "urgent_order_alert", order_id = o.Id });

        return o;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng dành riêng cho Khách hàng (chỉ lấy đơn của chính customerId đó).
    /// </summary>
    public async Task<object> GetCustomerOrdersAsync(
        int customerId,
        int page = 1,
        int itemPerpage = 10,
        string? search = null,
        string? status = null)          
    {
        if (page < 1) page = 1;
        if (itemPerpage < 1) itemPerpage = 10;

        var query = _db.Orders
            .AsNoTracking()                                
            .Include(o => o.Driver)                     
            .Where(o => o.CustomerId == customerId)
            .AsQueryable();

        // ============ FILTER: search (theo TrackingCode hoặc Id) ============
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant();
            bool isNumeric = int.TryParse(keyword, out int searchId);

            query = query.Where(o =>
                o.TrackingCode.ToLower().Contains(keyword) ||
                (isNumeric && o.Id == searchId));
        }

        // ============ FILTER: status ============
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status.Trim(), ignoreCase: true, out var parsedStatus)
                || !Enum.IsDefined(typeof(OrderStatus), parsedStatus))   
            {
                return new
                {
                    total_items = 0,
                    total_pages = 0,
                    current_page = page,
                    item_per_page = itemPerpage,
                    has_next = false,
                    has_prev = false,
                    data = Array.Empty<object>()
                };
            }

            query = query.Where(o => o.Status == parsedStatus);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)itemPerpage);

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * itemPerpage)
            .Take(itemPerpage)
            .ToListAsync();

        var items = orders.Select(o => o.ToResponse()).ToList();

        return new
        {
            total_items = totalItems,
            total_pages = totalPages,
            current_page = page,
            item_per_page = itemPerpage,
            has_next = page < totalPages,
            has_prev = page > 1,
            data = items
        };
    }

    /// <summary>
    /// Lấy toàn bộ danh sách đơn hàng trong hệ thống dành cho Admin (có hỗ trợ tìm kiếm và phân trang).
    /// </summary>
    public async Task<object> GetAllOrdersForAdminAsync(int page = 1, int itemPerpage = 10, string? search = null,string? status = null)
    {
        if (page < 1) page = 1;
        if (itemPerpage < 1) itemPerpage = 10;

        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Driver)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            bool isNumeric = int.TryParse(keyword, out int searchId);

            query = query.Where(o =>
                o.TrackingCode.ToLower().Contains(keyword) ||
                (o.Customer != null && o.Customer.Name.ToLower().Contains(keyword)) ||
                (isNumeric && o.Id == searchId)
            );
        }

        // ============ FILTER: status ============
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status.Trim(), ignoreCase: true, out var parsedStatus)
                || !Enum.IsDefined(typeof(OrderStatus), parsedStatus))   
            {
                return new
                {
                    total_items = 0,
                    total_pages = 0,
                    current_page = page,
                    item_per_page = itemPerpage,
                    has_next = false,
                    has_prev = false,
                    data = Array.Empty<object>()
                };
            }

            query = query.Where(o => o.Status == parsedStatus);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)itemPerpage);

        // Tính toán cờ phân trang
        bool hasNext = page < totalPages;
        bool hasPrev = page > 1;

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * itemPerpage)
            .Take(itemPerpage)
            .ToListAsync();

        var items = orders.Select(o => o.ToResponse()).ToList();

        return new
        {
            total_items = totalItems,
            total_pages = totalPages,
            current_page = page,
            item_per_page = itemPerpage,
            has_next = hasNext,
            has_prev = hasPrev,
            data = items
        };
    }

    /// <summary>
    /// Lấy thông tin chi tiết một đơn hàng theo ID, hỗ trợ kiểm tra quyền riêng tư của Khách hàng.
    /// </summary>
    public async Task<DeliveryOrder> GetOrderByIdAsync(int orderId, int userId, string userRole)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Driver)
            .AsQueryable();

        var order = await query.FirstOrDefaultAsync(o => o.Id == orderId);

        // 1. Kiểm tra xem đơn hàng có tồn tại hay không
        if (order == null)
        {
            throw new AppException(new OrderError(404, "Không tìm thấy đơn hàng."));
        }

        // 2. Phân quyền: Nếu user là Khách hàng (Customer), bắt buộc phải là chủ nhân của đơn hàng đó mới được xem
        if (userRole.ToUpper() == UserRole.Customer && order.CustomerId != userId)
        {
            throw new AppException(new OrderError(403, "Bạn không có quyền xem thông tin chi tiết của đơn hàng này."));
        }

        return order;
    }

    private static bool ValidGps(double lat, double lng) =>
        double.IsFinite(lat) && double.IsFinite(lng) && lat is >= -90 and <= 90 && lng is >= -180 and <= 180;
}