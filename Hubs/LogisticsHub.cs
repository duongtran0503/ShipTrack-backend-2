using System.Security.Claims;
using Logistics.Api.Data;
using Logistics.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Hubs;

/// <summary>
/// Hub SignalR quản lý kết nối real-time (thông báo đơn hàng, chat, vị trí tài xế) cho hệ thống Logistics.
/// Yêu cầu người dùng phải xác thực (Authorize) trước khi kết nối.
/// </summary>
[Authorize]
public class LogisticsHub(AppDbContext db) : Hub
{
    /// <summary>
    /// Xử lý sự kiện khi Client kết nối thành công vào Hub.
    /// Tự động thêm kết nối vào các Group riêng theo User ID và Role để dễ dàng bắn thông báo đích danh.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);

        // Thêm connection vào group cá nhân (dùng để gửi thông báo riêng cho user đó)
        if (id is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{id}");

        // Thêm connection vào group theo vai trò (dùng để gửi thông báo cho tất cả tài xế hoặc admin)
        if (role is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Cho phép Client tham gia vào một Group theo dõi đơn hàng cụ thể (dùng cho tính năng chat/tracking real-time).
    /// Kiểm tra quyền hạn xem đơn trước khi cho phép tham gia group.
    /// </summary>
    public async Task JoinOrder(int id)
    {
        int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var uid);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);

        // Kiểm tra xem user có phải là chủ đơn hàng, tài xế nhận đơn, hoặc là admin không
        var permitted = await db.Orders.AnyAsync(o => o.Id == id && (o.CustomerId == uid || o.DriverId == uid || role == "admin"));

        if (!permitted)
            throw new HubException("Bạn không có quyền theo dõi đơn này.");

        // Thêm connection vào group theo dõi đơn hàng
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{id}");
    }

    /// <summary>
    /// Rời khỏi Group theo dõi đơn hàng khi client không cần cập nhật real-time nữa.
    /// </summary>
    public Task LeaveOrder(int id) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{id}");
}