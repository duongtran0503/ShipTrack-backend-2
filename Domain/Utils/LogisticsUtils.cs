using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Logistics.Api.Utils;

/// <summary>
/// Lớp tiện ích tĩnh (Utility Class) chuyên trách các hàm toán học GPS (Haversine), 
/// thuật toán tính giá cước (Quote) và sinh mã JWT Token cho hệ thống.
/// </summary>
public static class LogisticsUtils
{
    /// <summary>
    /// Lấy User ID từ ClaimsPrincipal của người dùng đang đăng nhập.
    /// </summary>
    public static int UserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    /// <summary>
    /// Lấy Vai trò (Role) từ ClaimsPrincipal của người dùng đang đăng nhập.
    /// </summary>
    public static string Role(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? "";

    /// <summary>
    /// Tính khoảng cách đường chim bay (km) giữa 2 điểm tọa độ GPS dựa trên công thức Haversine.
    /// </summary>
    public static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        double rad = Math.PI / 180, dLat = (lat2 - lat1) * rad, dLon = (lng2 - lng1) * rad;
        double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1 * rad) * Math.Cos(lat2 * rad) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 6371 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>
    /// Tính toán giá cước tự động phía Backend dựa trên quãng đường GPS ước lượng, loại dịch vụ và phụ phí.
    /// </summary>
    public static (decimal basePrice, decimal totalPrice, double distanceKm) Quote(CreateOrderRequest r)
    {
        double d = Haversine(r.PickupLat, r.PickupLng, r.DropoffLat, r.DropoffLng) * 1.3;
        d = Math.Max(0.5, Math.Round(d, 1));

        decimal price = r.ServiceType switch
        {
            "container" => 2600000m + Math.Max(0, (decimal)Math.Ceiling(d - 20)) * 33000m,
            "truck" => 150000m + Math.Max(0, (decimal)Math.Ceiling(d - 5)) * 15000m,
            _ => d <= 3 ? 16000m : d <= 5 ? 25000m : 25000m + (decimal)Math.Ceiling(d - 5) * 6500m
        };

        var extra = (r.IsBulky && r.ServiceType != "container" ? 20000m : 0) + (r.IsDoorDelivery ? 10000m : 0);
        var tip = Math.Max(0, r.TipAmount);
        var discount = (r.PromotionCode?.Trim().ToUpperInvariant() is "SGU" or "ITVINA") ? 15000m : 0m;

        return (price, Math.Max(0, price + extra + tip - discount), d);
    }

    /// <summary>
    /// Sinh mã JWT Token độc lập với thời hạn 8 giờ.
    /// </summary>
    public static string Token(User u, IConfiguration cfg)
    {
        var secret = cfg["JWT_SECRET"] ?? throw new InvalidOperationException("Thiếu JWT_SECRET. Hãy cấu hình biến môi trường JWT_SECRET (ít nhất 32 ký tự).");
        if (Encoding.UTF8.GetByteCount(secret) < 32) throw new InvalidOperationException("JWT_SECRET phải đủ 32 bytes.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: cfg["Jwt:Issuer"],
            audience: cfg["Jwt:Audience"],
            claims: [
                new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
                new Claim(ClaimTypes.Role, u.Role),
                new Claim(ClaimTypes.Name, u.Name)
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: cred
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}