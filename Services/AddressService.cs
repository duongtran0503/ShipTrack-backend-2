using Logistics.Api.Data;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.DTOs.Responses;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services;

public class AddressService
{
    private readonly AppDbContext _db;

    public AddressService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách sổ địa chỉ của một User.
    /// </summary>
    public async Task<List<AddressResponse>> GetMyAddressesAsync(int ownerId)
    {
        var addresses = await _db.Addresses
            .Where(a => a.OwnerId == ownerId)
            .OrderByDescending(a => a.IsDefault) // Đưa địa chỉ mặc định lên đầu
            .ThenByDescending(a => a.CreatedAtUtc)
            .ToListAsync();

        return addresses.Select(a => a.ToResponse()).ToList();
    }

    /// <summary>
    /// Lấy chi tiết một địa chỉ theo ID (Có kiểm tra quyền sở hữu).
    /// </summary>
    public async Task<AddressResponse> GetAddressByIdAsync(int addressId, int ownerId)
    {
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == addressId);

        if (address == null)
            throw new AppException(AddressError.NotFound);

        if (address.OwnerId != ownerId)
            throw new AppException(AddressError.Forbidden);

        return address.ToResponse();
    }

    /// <summary>
    /// Tạo mới một địa chỉ vào sổ địa chỉ (Có kiểm tra trùng SĐT và quản lý địa chỉ mặc định duy nhất).
    /// </summary>
    public async Task<AddressResponse> CreateAddressAsync(int ownerId, UpsertAddressRequest req)
    {
        var cleanPhone = req.ContactPhone.Trim();

        // 1. Kiểm tra xem số điện thoại này đã tồn tại trong sổ địa chỉ của user này chưa
        bool phoneExists = await _db.Addresses
            .AnyAsync(a => a.OwnerId == ownerId && a.ContactPhone == cleanPhone);

        if (phoneExists)
        {
            throw new AppException(AddressError.DuplicatePhone);
        }

        // 2. Nếu user chưa có địa chỉ nào, hoặc request yêu cầu set IsDefault = true
        var hasAnyAddress = await _db.Addresses.AnyAsync(a => a.OwnerId == ownerId);
        bool shouldBeDefault = req.IsDefault || !hasAnyAddress; // Nếu là địa chỉ đầu tiên, tự động thành mặc định

        if (shouldBeDefault)
        {
            // Reset tất cả các địa chỉ cũ của user này về false
            var existingDefaults = await _db.Addresses
                .Where(a => a.OwnerId == ownerId && a.IsDefault)
                .ToListAsync();

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
            }
        }

        var address = new Address
        {
            OwnerId = ownerId,
            Label = req.Label.Trim(),
            ContactName = req.ContactName.Trim(),
            ContactPhone = cleanPhone,
            FullAddress = req.FullAddress.Trim(),
            IsDefault = shouldBeDefault,
            Lat = req.Lat,
            Lng = req.Lng,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync();

        return address.ToResponse();
    }

    /// <summary>
    /// Cập nhật thông tin địa chỉ (Có kiểm tra trùng SĐT và quản lý địa chỉ mặc định duy nhất).
    /// </summary>
    public async Task<AddressResponse> UpdateAddressAsync(int addressId, int ownerId, UpsertAddressRequest req)
    {
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == addressId);

        if (address == null)
            throw new AppException(AddressError.NotFound);

        if (address.OwnerId != ownerId)
            throw new AppException(AddressError.Forbidden);

        var cleanPhone = req.ContactPhone.Trim();

        // 1. Kiểm tra nếu đổi số điện thoại mới mà bị trùng với bản ghi khác của chính user này
        bool phoneExists = await _db.Addresses
            .AnyAsync(a => a.OwnerId == ownerId && a.Id != addressId && a.ContactPhone == cleanPhone);

        if (phoneExists)
        {
            throw new AppException(AddressError.DuplicatePhone);
        }

        // 2. Xử lý đổi trạng thái IsDefault
        if (req.IsDefault)
        {
            // Đưa tất cả địa chỉ khác về false
            var existingDefaults = await _db.Addresses
                .Where(a => a.OwnerId == ownerId && a.Id != addressId && a.IsDefault)
                .ToListAsync();

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
            }
        }
        else
        {
            // Đảm bảo không cho phép user bỏ chọn mặc định nếu đây là địa chỉ mặc định duy nhất còn lại
            // (Tùy nghiệp vụ: nếu muốn bắt buộc luôn phải có 1 địa chỉ mặc định)
            bool isCurrentDefault = address.IsDefault;
            if (isCurrentDefault)
            {
                // Kiểm tra xem còn địa chỉ nào khác không, nếu có thể giữ nguyên hoặc bắt buộc phải có 1 cái mặc định
                // Tạm thời cho phép cập nhật, nhưng nếu bạn muốn ép luôn có 1 cái mặc định thì bật cờ này lên.
            }
        }

        address.Label = req.Label.Trim();
        address.ContactName = req.ContactName.Trim();
        address.ContactPhone = cleanPhone;
        address.FullAddress = req.FullAddress.Trim();
        address.IsDefault = req.IsDefault;
        address.Lat = req.Lat;
        address.Lng = req.Lng;
        address.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return address.ToResponse();
    }

    /// <summary>
    /// Xóa một địa chỉ khỏi sổ địa chỉ.
    /// </summary>
    public async Task DeleteAddressAsync(int addressId, int ownerId)
    {
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == addressId);

        if (address == null)
            throw new AppException(AddressError.NotFound);

        if (address.OwnerId != ownerId)
            throw new AppException(AddressError.Forbidden);

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();
    }
}