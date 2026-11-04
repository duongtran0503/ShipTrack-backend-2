using Logistics.Api.Data;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services;

public class DriverVehicleService
{
    private readonly AppDbContext _db;

    public DriverVehicleService(AppDbContext db) => _db = db;



    /// <summary>
    /// Admin duyệt (verify) phương tiện cho tài xế.
    /// </summary>
    public async Task<DriverVehicleResponse> VerifyVehicleAsync(int vehicleId, bool isVerified)
    {
        var vehicle = await _db.DriverVehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            throw new AppException(new AuthError(404, "Không tìm thấy phương tiện."));
        }

        vehicle.IsVerified = isVerified;
        await _db.SaveChangesAsync();

        return vehicle.ToResponse();
    }


    /// <summary>
    /// Admin lấy danh sách toàn bộ xe trong hệ thống (hỗ trợ phân trang, tìm kiếm và lọc).
    /// </summary>
    public async Task<(List<DriverVehicleResponse> Items, int TotalCount)> AdminGetAllVehiclesAsync(bool? isVerified, int pageNumber, int pageSize, string? searchTerm)
    {
        var query = _db.DriverVehicles.AsQueryable();

        // 1. Lọc theo trạng thái duyệt nếu có
        if (isVerified.HasValue)
        {
            query = query.Where(v => v.IsVerified == isVerified.Value);
        }

        // 2. Tìm kiếm theo biển số hoặc hãng xe nếu có searchTerm
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpper();
            query = query.Where(v => v.LicensePlate.ToUpper().Contains(term) || (v.Brand != null && v.Brand.ToUpper().Contains(term)));
        }

        // 3. Đếm tổng số bản ghi trước khi phân trang
        var totalCount = await query.CountAsync();

        // 4. Phân trang và lấy dữ liệu
        var vehicles = await query
            .OrderByDescending(v => v.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = vehicles.Select(v => v.ToResponse()).ToList();

        return (items, totalCount);
    }
    /// <summary>
    /// Lấy danh sách phương tiện của tài xế.
    /// </summary>
    public async Task<List<DriverVehicleResponse>> GetMyVehiclesAsync(int driverId)
    {
        var vehicles = await _db.DriverVehicles
            .Where(v => v.DriverId == driverId)
            .OrderByDescending(v => v.IsDefault)
            .ThenByDescending(v => v.CreatedAtUtc)
            .ToListAsync();

        return vehicles.Select(v => v.ToResponse()).ToList();
    }

    /// <summary>
    /// Thêm phương tiện mới (Đăng ký xe mới / Đổi xe mới).
    /// </summary>
    public async Task<DriverVehicleResponse> CreateVehicleAsync(int driverId, UpsertVehicleRequest req)
    {
        var cleanPlate = req.LicensePlate.Trim().ToUpper();

        // Kiểm tra trùng biển số xe trong hệ thống
        bool plateExists = await _db.DriverVehicles.AnyAsync(v => v.LicensePlate == cleanPlate);
        if (plateExists)
        {
            throw new AppException(new AuthError(400, "Biển số xe này đã được đăng ký trong hệ thống."));
        }

        // Tự động set xe đầu tiên hoặc nếu tài xế muốn đổi sang xe mới làm mặc định
        var hasAnyVehicle = await _db.DriverVehicles.AnyAsync(v => v.DriverId == driverId);
        bool shouldBeDefault = !hasAnyVehicle; // Nếu là xe đầu tiên, mặc định active luôn

        if (shouldBeDefault)
        {
            // Reset các xe cũ về không mặc định
            var existingDefaults = await _db.DriverVehicles
                .Where(v => v.DriverId == driverId && v.IsDefault)
                .ToListAsync();

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
            }
        }

        var vehicle = new DriverVehicle
        {
            DriverId = driverId,
            VehicleType = req.VehicleType,
            LicensePlate = cleanPlate,
            Brand = req.Brand?.Trim(),
            Color = req.Color?.Trim(),
            MaxWeightKg = req.MaxWeightKg,
            IsDefault = shouldBeDefault,
            IsVerified = false, // Xe mới đăng ký chờ admin duyệt
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.DriverVehicles.Add(vehicle);

        // Nếu là xe mặc định đầu tiên, cập nhật luôn sang bảng User để đồng bộ dữ liệu cũ
        if (shouldBeDefault)
        {
            var user = await _db.Users.FindAsync(driverId);
            if (user != null)
            {
                user.LicensePlate = cleanPlate;
            }
        }

        await _db.SaveChangesAsync();
        return vehicle.ToResponse();
    }

    /// <summary>
    /// Đặt phương tiện làm xe mặc định (đang sử dụng chính để chạy đơn).
    /// </summary>
    public async Task<DriverVehicleResponse> SetDefaultVehicleAsync(int vehicleId, int driverId)
    {
        var targetVehicle = await _db.DriverVehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (targetVehicle == null || targetVehicle.DriverId != driverId)
        {
            throw new AppException(new AuthError(404, "Không tìm thấy phương tiện."));
        }

        // Đưa tất cả xe của driver này về IsDefault = false
        var allVehicles = await _db.DriverVehicles.Where(v => v.DriverId == driverId).ToListAsync();
        foreach (var v in allVehicles)
        {
            v.IsDefault = (v.Id == vehicleId);
        }

        // Đồng bộ biển số sang bảng User
        var user = await _db.Users.FindAsync(driverId);
        if (user != null)
        {
            user.LicensePlate = targetVehicle.LicensePlate;
        }

        await _db.SaveChangesAsync();
        return targetVehicle.ToResponse();
    }

    /// <summary>
    /// Xóa phương tiện khỏi danh sách.
    /// </summary>
    public async Task DeleteVehicleAsync(int vehicleId, int driverId)
    {
        var vehicle = await _db.DriverVehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null || vehicle.DriverId != driverId)
        {
            throw new AppException(new AuthError(404, "Không tìm thấy phương tiện."));
        }

        if (vehicle.IsDefault)
        {
            throw new AppException(new AuthError(400, "Không thể xóa phương tiện đang được chọn làm mặc định. Vui lòng chuyển xe mặc định trước."));
        }

        _db.DriverVehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
    }
    /// <summary>
    /// Lấy thông tin phương tiện mặc định và đã được duyệt (đang sử dụng) của tài xế qua token.
    /// </summary>
    public async Task<DriverVehicleResponse?> GetCurrentActiveVehicleAsync(int driverId)
    {
        var activeVehicle = await _db.DriverVehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.DriverId == driverId && v.IsDefault && v.IsVerified);

        // Nếu không có xe default nào được duyệt, có thể tìm xe đã được duyệt bất kỳ làm fallback
        if (activeVehicle == null)
        {
            activeVehicle = await _db.DriverVehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.DriverId == driverId && v.IsVerified);
        }

        return activeVehicle?.ToResponse();
    }


}