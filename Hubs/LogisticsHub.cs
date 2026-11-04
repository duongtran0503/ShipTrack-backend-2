using System.Collections.Concurrent;
using System.Security.Claims;
using Logistics.Api.Data;
using Logistics.Api.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Hubs;

/// <summary>
/// Hub SignalR quản lý kết nối real-time (thông báo đơn hàng, chat, vị trí tài xế).
/// Yêu cầu người dùng phải xác thực (Authorize) trước khi kết nối.
/// </summary>
[Authorize]
public class LogisticsHub(AppDbContext db, ILogger<LogisticsHub> logger) : Hub
{
    /// <summary>
    /// Đếm số connection đang mở của mỗi driver (in-memory).
    /// Driver mở N tab → count = N. Chỉ khi count = 0 mới set Online = false.
    /// </summary>
    private static readonly ConcurrentDictionary<int, int> DriverConnections = new();

    // ==================== CONNECT ====================

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId(Context);
        var role = GetRole(Context);
        if (userId is null) { await base.OnConnectedAsync(); return; }

        // Join group theo role
        await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role?.ToLowerInvariant()}");

        // Join group riêng của user (sync tab)
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        if (role?.StartsWith("DRIVER", StringComparison.OrdinalIgnoreCase) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"driver_{userId}");

            var count = DriverConnections.AddOrUpdate(userId.Value, 1, (_, c) => c + 1);
            logger.LogInformation(
                "Driver {DriverId} connected. Active connections: {Count}",
                userId, count);
        }

        if (string.Equals(role, UserRole.Customer, StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"customer_{userId}");
        }

        await base.OnConnectedAsync();
    }

    // ==================== DISCONNECT ====================

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId(Context);
        var role = GetRole(Context);

        if (role?.StartsWith("DRIVER", StringComparison.OrdinalIgnoreCase) == true
            && userId is not null)
        {
            var count = DriverConnections.AddOrUpdate(
                userId.Value, 0, (_, c) => Math.Max(0, c - 1));

            logger.LogInformation(
                "Driver {DriverId} disconnected. Remaining connections: {Count}",
                userId, count);

            // Chỉ set offline khi đóng hết tab
            if (count == 0)
            {
                await HandleDriverFullyDisconnectedAsync(userId.Value);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Xử lý khi driver đóng hết connection: set Online = false + broadcast.
    /// </summary>
    private async Task HandleDriverFullyDisconnectedAsync(int driverId)
    {
        var driver = await db.Users.FindAsync(driverId);
        if (driver is null || !driver.Online)
            return;

        driver.Online = false;
        await db.SaveChangesAsync();

        logger.LogInformation("Driver {DriverId} set OFFLINE (all connections closed)", driverId);

        var payload = new
        {
            driverId = driver.Id,
            driverName = driver.Name,
            isOnline = false,
            changedAtUtc = DateTime.UtcNow,
        };

        // Broadcast cho admin
        await Clients.Group("role_admin").SendAsync("DriverStatusChanged", payload);

        // Broadcast cho customer đang có đơn active của driver này
        var customerIds = await db.Orders
            .AsNoTracking()
            .Where(o => o.DriverId == driverId
                     && o.Status != OrderStatus.COMPLETED
                     && o.Status != OrderStatus.CANCELLED)
            .Select(o => o.CustomerId)
            .Distinct()
            .ToListAsync();

        foreach (var cid in customerIds)
        {
            await Clients.Group($"customer_{cid}")
                .SendAsync("DriverStatusChanged", payload);
        }
    }

    // ==================== ORDER GROUP ====================

    public async Task JoinOrderGroup(int id)
    {
        int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var uid);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        var isAdmin = string.Equals(role, UserRole.Admin, StringComparison.OrdinalIgnoreCase);

        var permitted = await db.Orders.AnyAsync(o =>
            o.Id == id && (o.CustomerId == uid || o.DriverId == uid || isAdmin));

        if (!permitted)
            throw new HubException("Bạn không có quyền theo dõi đơn này.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{id}");
    }

    public Task LeaveOrderGroup(int id) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{id}");

    // ==================== HELPERS ====================

    private static int? GetUserId(HubCallerContext ctx)
    {
        var raw = ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? ctx.User?.FindFirst("sub")?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    private static string? GetRole(HubCallerContext ctx) =>
        ctx.User?.FindFirst(ClaimTypes.Role)?.Value
        ?? ctx.User?.FindFirst("role")?.Value;
}