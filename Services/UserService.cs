//using Logistics.Api.Data;
//using Logistics.Api.Domain.Entities;
//using Logistics.Api.Hubs;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;

//namespace Logistics.Api.Services;

///// <summary>
///// Lớp dịch vụ xử lý logic nghiệp vụ liên quan đến Người dùng (User), 
///// cập nhật thông tin cá nhân, vị trí GPS tài xế, quản lý ví mô phỏng và hộp thư hỗ trợ.
///// </summary>
//public class UserService
//{
//    private readonly AppDbContext _db;
//    private readonly IHubContext<LogisticsHub> _hub;
//    private readonly IHostEnvironment _env;

//    public UserService(AppDbContext db, IHubContext<LogisticsHub> hub, IHostEnvironment env)
//    {
//        _db = db;
//        _hub = hub;
//        _env = env;
//    }

//    /// <summary>
//    /// Lấy thông tin chi tiết người dùng theo ID.
//    /// </summary>
//    public async Task<User?> GetUserByIdAsync(int id)
//    {
//        return await _db.Users.FindAsync(id);
//    }

//    /// <summary>
//    /// Cập nhật thông tin cá nhân (Profile) của người dùng (tên, số điện thoại, biển số xe nếu là tài xế).
//    /// </summary>
//    public async Task<User?> UpdateProfileAsync(int userId, ProfileRequest req)
//    {
//        var u = await _db.Users.FindAsync(userId);
//        if (u == null) return null;

//        if (string.IsNullOrWhiteSpace(req.Name)) return null;

//        u.Name = req.Name.Trim();
//        u.Phone = req.Phone?.Trim() ?? "";

//        // Nếu là tài xế thì cho phép cập nhật thêm biển số xe
//        if (u.Role.StartsWith("driver"))
//        {
//            u.LicensePlate = req.LicensePlate?.Trim() ?? u.LicensePlate;
//        }

//        await _db.SaveChangesAsync();
//        return u;
//    }

//    /// <summary>
//    /// Cập nhật tọa độ GPS trực tiếp và trạng thái sẵn sàng nhận đơn (Online/Offline) của tài xế.
//    /// </summary>
//    public async Task<User?> UpdateDriverLocationAsync(int driverId, LocationRequest req)
//    {
//        var u = await _db.Users.FindAsync(driverId);
//        if (u == null || !u.Role.StartsWith("driver")) return null;

//        u.Latitude = req.Latitude;
//        u.Longitude = req.Longitude;
//        u.Online = req.Online;

//        await _db.SaveChangesAsync();
//        return u;
//    }

//    /// <summary>
//    /// Lấy danh sách lịch sử giao dịch ví của người dùng (tối đa 200 giao dịch gần nhất).
//    /// </summary>
//    public async Task<List<WalletTransaction>> GetUserTransactionsAsync(int userId)
//    {
//        return await _db.WalletTransactions
//            .Where(t => t.UserId == userId)
//            .OrderByDescending(t => t.CreatedAtUtc)
//            .Take(200)
//            .ToListAsync();
//    }

//    /// <summary>
//    /// Nạp tiền demo vào ví (Chỉ cho phép ở môi trường Development, tối đa 10 triệu đồng).
//    /// </summary>
//    public async Task<(bool Success, string? Error, User? User)> TopUpAsync(int userId, decimal amount)
//    {
//        if (!_env.IsDevelopment())
//            return (false, "Nạp tiền demo chỉ có ở môi trường Development.", null);

//        if (amount <= 0 || amount > 10000000 || amount != decimal.Truncate(amount))
//            return (false, "Số tiền nạp phải > 0 và <= 10 triệu.", null);

//        var u = await _db.Users.FindAsync(userId);
//        if (u == null) return (false, "Không tìm thấy người dùng.", null);

//        // Cộng tiền vào ví và ghi nhận lịch sử giao dịch
//        u.Balance += amount;
//        _db.WalletTransactions.Add(new WalletTransaction
//        {
//            UserId = userId,
//            Amount = amount,
//            Type = "topup",
//            Description = "Nạp ví mô phỏng - không phải tiền thật"
//        });

//        await _db.SaveChangesAsync();

//        // Bắn thông báo biến động số dư real-time qua SignalR tới client
//        await _hub.Clients.Group($"user_{userId}").SendAsync("RealtimeEvent", new { eventName = "balance_changed" });

//        return (true, null, u);
//    }

//    /// <summary>
//    /// Rút tiền demo khỏi ví (Kiểm tra số dư và môi trường Development).
//    /// </summary>
//    public async Task<(bool Success, string? Error, User? User)> WithdrawAsync(int userId, decimal amount)
//    {
//        if (!_env.IsDevelopment())
//            return (false, "Rút tiền demo chỉ có ở môi trường Development.", null);

//        if (amount <= 0 || amount > 10000000 || amount != decimal.Truncate(amount))
//            return (false, "Số tiền rút không hợp lệ.", null);

//        var u = await _db.Users.FindAsync(userId);
//        if (u == null) return (false, "Không tìm thấy người dùng.", null);
//        if (u.Balance < amount) return (false, "Số dư không đủ.", null);

//        // Trừ tiền ví và ghi nhận lịch sử giao dịch
//        u.Balance -= amount;
//        _db.WalletTransactions.Add(new WalletTransaction
//        {
//            UserId = userId,
//            Amount = -amount,
//            Type = "withdrawal",
//            Description = "Rút ví mô phỏng"
//        });

//        await _db.SaveChangesAsync();

//        // Bắn thông báo biến động số dư real-time qua SignalR tới client
//        await _hub.Clients.Group($"user_{userId}").SendAsync("RealtimeEvent", new { eventName = "balance_changed" });

//        return (true, null, u);
//    }

//    /// <summary>
//    /// Lấy danh sách tin nhắn hỗ trợ (Support messages) của người dùng.
//    /// </summary>
//    public async Task<List<SupportMessage>> GetSupportMessagesAsync(int userId)
//    {
//        return await _db.SupportMessages
//            .Where(m => m.UserId == userId)
//            .OrderBy(m => m.CreatedAtUtc)
//            .ToListAsync();
//    }

//    /// <summary>
//    /// Gửi tin nhắn yêu cầu hỗ trợ mới và tự động tạo phản hồi giả lập từ Bot hệ thống.
//    /// </summary>
//    public async Task SendSupportMessageAsync(int userId, string content)
//    {
//        // Lưu tin nhắn từ người dùng
//        _db.SupportMessages.Add(new SupportMessage { UserId = userId, SenderType = "user", Content = content.Trim() });

//        // Lưu tin nhắn tự động phản hồi từ hệ thống/bot
//        _db.SupportMessages.Add(new SupportMessage { UserId = userId, SenderType = "bot", Content = "Hệ thống đã ghi nhận tin nhắn. Quản trị viên sẽ phản hồi khi có thể." });

//        await _db.SaveChangesAsync();

//        // Thông báo real-time cho user và nhóm quản trị viên (Admin)
//        await _hub.Clients.Group($"user_{userId}").SendAsync("RealtimeEvent", new { eventName = "new_support_msg" });
//        await _hub.Clients.Group("role_admin").SendAsync("RealtimeEvent", new { eventName = "new_support_msg", user_id = userId });
//    }
//}