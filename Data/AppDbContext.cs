using Logistics.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Data;

/// <summary>
/// Lớp DbContext quản lý kết nối CSDL và ánh xạ các bảng dữ liệu của hệ thống.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Khai báo các bảng (DbSet) tương ứng với các Entity trong domain
    public DbSet<User> Users => Set<User>();
    public DbSet<DeliveryOrder> Orders => Set<DeliveryOrder>();
    public DbSet<OrderStatusHistory> StatusHistory => Set<OrderStatusHistory>();
    public DbSet<OrderMessage> OrderMessages => Set<OrderMessage>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<DriverVehicle> DriverVehicles => Set<DriverVehicle>();
    public DbSet<DriverServiceArea> DriverServiceAreas => Set<DriverServiceArea>();
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        // --- CẤU HÌNH INDEX (Tối ưu hiệu năng tìm kiếm) ---

        // Email người dùng phải là duy nhất (không được trùng nhau)
        m.Entity<User>().HasIndex(x => x.Email).IsUnique();

        // Tạo Index kết hợp trạng thái và thời gian tạo đơn để lọc dữ liệu nhanh hơn
        m.Entity<DeliveryOrder>().HasIndex(x => new { x.Status, x.CreatedAtUtc });

        // --- CẤU HÌNH QUAN HỆ & RÀNG BUỘC (Foreign Keys & Delete Behavior) ---

        // Khách hàng (Customer) - Chống xóa lan truyền (Restrict) để tránh mất dữ liệu đơn hàng
        m.Entity<DeliveryOrder>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tài xế (Driver) - Chống xóa lan truyền (Restrict)
        m.Entity<DeliveryOrder>()
            .HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index tìm kiếm tin nhắn theo Mã đơn hàng và thời gian
        m.Entity<OrderMessage>().HasIndex(x => new { x.OrderId, x.CreatedAtUtc });

        // Index quản lý lịch sử giao dịch ví theo Người dùng và thời gian
        m.Entity<WalletTransaction>().HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        // Mỗi đơn hàng chỉ được phép có tối đa 1 đánh giá (Unique)
        m.Entity<Review>().HasIndex(x => x.OrderId).IsUnique();

        // Index query theo Owner (chủ danh bạ)
        m.Entity<Address>().HasIndex(x => x.OwnerId);

        // Partial unique index: mỗi Owner tối đa 1 địa chỉ mặc định
        // (SQLite hỗ trợ partial index từ 3.8.0+)
        m.Entity<Address>()
            .HasIndex(x => x.OwnerId)
            .HasDatabaseName("IX_Addresses_OwnerId_IsDefault")
            .HasFilter("\"IsDefault\" = 1");

        // FK: Owner — xóa Owner thì xóa hết địa chỉ trong danh bạ
        m.Entity<Address>()
            .HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);



        // ============ DRIVER VEHICLE ============
        m.Entity<DriverVehicle>().HasIndex(x => x.DriverId);

        // Partial unique index: mỗi driver tối đa 1 xe mặc định
        m.Entity<DriverVehicle>()
            .HasIndex(x => x.DriverId)
            .HasDatabaseName("IX_DriverVehicles_DriverId_IsDefault")
            .HasFilter("\"IsDefault\" = 1");

        m.Entity<DriverVehicle>()
            .HasOne(x => x.Driver).WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============ DRIVER SERVICE AREA ============
        m.Entity<DriverServiceArea>().HasIndex(x => x.DriverId);

        m.Entity<DriverServiceArea>()
            .HasIndex(x => x.DriverId)
            .HasDatabaseName("IX_DriverServiceAreas_DriverId_IsDefault")
            .HasFilter("\"IsDefault\" = 1");

        m.Entity<DriverServiceArea>()
            .HasOne(x => x.Driver).WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- CẤU HÌNH KIỂU DỮ LIỆU TIỀN TỆ (Value Converters: decimal <-> long) ---
        // Giúp lưu các giá trị tiền tệ (decimal) dưới dạng số nguyên (long) trong SQLite để tính toán chính xác, tránh sai số thập phân
        m.Entity<User>().Property(x => x.Balance).HasConversion(v => (long)v, v => (decimal)v);
        m.Entity<DeliveryOrder>().Property(x => x.BasePrice).HasConversion(v => (long)v, v => (decimal)v);
        m.Entity<DeliveryOrder>().Property(x => x.TotalPrice).HasConversion(v => (long)v, v => (decimal)v);
        m.Entity<DeliveryOrder>().Property(x => x.DiscountAmount).HasConversion(v => (long)v, v => (decimal)v);
        m.Entity<DeliveryOrder>().Property(x => x.TipAmount).HasConversion(v => (long)v, v => (decimal)v);
        m.Entity<DeliveryOrder>().Property(x => x.CodAmount).HasConversion(v => (long)v, v => (decimal)v);
        m.Entity<WalletTransaction>().Property(x => x.Amount).HasConversion(v => (long)v, v => (decimal)v);
    }
}