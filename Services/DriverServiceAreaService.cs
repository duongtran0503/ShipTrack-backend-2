using Logistics.Api.Data;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services;

public class DriverServiceAreaService
{
    private readonly AppDbContext _db;

    public DriverServiceAreaService(AppDbContext db) => _db = db;

    public async Task<List<ServiceAreaResponse>> GetMyServiceAreasAsync(int driverId)
    {
        var areas = await _db.DriverServiceAreas
            .Where(a => a.DriverId == driverId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ToListAsync();

        return areas.Select(a => a.ToResponse()).ToList();
    }

    public async Task<ServiceAreaResponse> CreateServiceAreaAsync(int driverId, UpsertServiceAreaRequest req)
    {
        var hasAny = await _db.DriverServiceAreas.AnyAsync(a => a.DriverId == driverId);
        bool shouldBeDefault = req.IsDefault || !hasAny; // Nếu là vùng đầu tiên tự động thành mặc định

        if (shouldBeDefault)
        {
            var existingDefaults = await _db.DriverServiceAreas
                .Where(a => a.DriverId == driverId && a.IsDefault)
                .ToListAsync();

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
            }
        }

        var area = new DriverServiceArea
        {
            DriverId = driverId,
            Name = req.Name.Trim(),
            CenterLat = req.CenterLat,
            CenterLng = req.CenterLng,
            RadiusKm = req.RadiusKm,
            IsDefault = shouldBeDefault,
            IsActive = req.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.DriverServiceAreas.Add(area);
        await _db.SaveChangesAsync();

        return area.ToResponse();
    }

    public async Task<ServiceAreaResponse> UpdateServiceAreaAsync(int areaId, int driverId, UpsertServiceAreaRequest req)
    {
        var area = await _db.DriverServiceAreas.FirstOrDefaultAsync(a => a.Id == areaId);
        if (area == null || area.DriverId != driverId)
            throw new AppException(new AuthError(404, "Không tìm thấy khu vực phục vụ."));

        if (req.IsDefault)
        {
            var existingDefaults = await _db.DriverServiceAreas
                .Where(a => a.DriverId == driverId && a.Id != areaId && a.IsDefault)
                .ToListAsync();

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
            }
        }

        area.Name = req.Name.Trim();
        area.CenterLat = req.CenterLat;
        area.CenterLng = req.CenterLng;
        area.RadiusKm = req.RadiusKm;
        area.IsDefault = req.IsDefault;
        area.IsActive = req.IsActive;

        await _db.SaveChangesAsync();
        return area.ToResponse();
    }

    public async Task DeleteServiceAreaAsync(int areaId, int driverId)
    {
        var area = await _db.DriverServiceAreas.FirstOrDefaultAsync(a => a.Id == areaId);
        if (area == null || area.DriverId != driverId)
            throw new AppException(new AuthError(404, "Không tìm thấy khu vực phục vụ."));

        _db.DriverServiceAreas.Remove(area);
        await _db.SaveChangesAsync();
    }
}