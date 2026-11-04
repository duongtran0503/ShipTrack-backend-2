using Logistics.Api.Data;
using Logistics.Api.Domain.Constants;
using Logistics.Api.Domain.DTOs.Mappings;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions;
using Logistics.Api.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services;

/// <summary>
/// Lớp dịch vụ xử lý logic nghiệp vụ liên quan đến Người dùng (User), 
/// cập nhật thông tin cá nhân, vị trí GPS tài xế, quản lý ví mô phỏng và hộp thư hỗ trợ.
/// </summary>
public class UserService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<LogisticsHub> _hub;
    private readonly PasswordHasher<User> _hasher;
    private readonly IHostEnvironment _env;

    public UserService(AppDbContext db, IHubContext<LogisticsHub> hub, IHostEnvironment env, PasswordHasher<User> hasher)
    {
        _db = db;
        _hub = hub;
        _env = env;
        _hasher = hasher;
    }

    /// <summary>
    /// Lấy thông tin hồ sơ cá nhân của user đang đăng nhập (Xử lý hoàn toàn tại Service).
    /// </summary>
    public async Task<UserResponse> GetMyProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
        {
            throw new AppException(new AuthError(404, "Không tìm thấy thông tin người dùng."));
        }

        // Thực hiện mapping ngay tại Service hoặc trả về entity tùy cấu trúc dự án của bạn. 
        // Ở đây ta dùng extension method .ToResponse() để trả về DTO chuẩn.
        return user.ToResponse();
    }

    public async Task<User> UpdateAsync(int id, UpdateUserRequest req)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new AppException(new AuthError(404, "Người dùng không tồn tại."));
        user.Name = req.Name.Trim();
        user.Phone = req.Phone.Trim();
        user.LicensePlate = req.LicensePlate?.Trim() ?? "";
        user.Balance = req.Balance;
        user.IsActive = req.IsActive;
        user.BanReason = req.IsActive
            ? ""                                        
            : req.BanReason?.Trim() ?? "";

        await _db.SaveChangesAsync();
        return user;
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new AppException(new AuthError(404, "Người dùng không tồn tại."));

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword) != PasswordVerificationResult.Failed)
        {
            throw new AppException(new AuthError(400, "Mật khẩu hiện tại không đúng."));
        }
        

        user.PasswordHash = _hasher.HashPassword(user,req.NewPassword);
        await _db.SaveChangesAsync();
    }


    /// <summary>
    /// Lấy thống kê nhanh cho Customer Dashboard.
    /// </summary>
    public async Task<CustomerDashboardStatsResponse> GetDashboardStatsAsync(int customerId)
    {
        // Base query: chỉ đơn của customer hiện tại
        var baseQuery = _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == customerId);

        // Đếm song song để giảm round-trip DB
        var totalOrdersTask = baseQuery.CountAsync();

        var completedOrdersTask = baseQuery
            .CountAsync(o => o.Status == OrderStatus.COMPLETED);

        // "Đang hoạt động" = mọi trạng thái TRỪ COMPLETED và CANCELLED
        var activeOrdersTask = baseQuery
            .CountAsync(o => o.Status != OrderStatus.COMPLETED
                          && o.Status != OrderStatus.CANCELLED);

    

        var totalSpentTask = baseQuery
            .Where(o => o.Status == OrderStatus.COMPLETED)
            .SumAsync(o => (decimal?)o.TotalPrice);

        await Task.WhenAll(totalOrdersTask, completedOrdersTask, activeOrdersTask, totalSpentTask);




        return new CustomerDashboardStatsResponse
        {
            ActiveOrders = activeOrdersTask.Result,
            CompletedOrders = completedOrdersTask.Result,
            TotalOrders = totalOrdersTask.Result,
            TotalSpent = totalSpentTask.Result ?? 0m,   
        };
    }


}