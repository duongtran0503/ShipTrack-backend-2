using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Logistics.Api.Data;
using Logistics.Api.Domain.DTOs.Requests;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Logistics.Api.Services;

/// <summary>
/// Lớp dịch vụ xử lý logic nghiệp vụ xác thực (Đăng ký, Đăng nhập và Sinh mã JWT Token).
/// Tích hợp cơ chế ném ngoại lệ AppEndpoints chuẩn hóa.
/// </summary>
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, PasswordHasher<User> hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    /// <summary>
    /// Xử lý nghiệp vụ đăng ký tài khoản mới cho người dùng.
    /// Sẽ ném AppException nếu dữ liệu không hợp lệ hoặc email đã tồn tại.
    /// </summary>
    public async Task<object> RegisterAsync(RegisterRequest req)
    {
        // 1. Chuẩn hóa và kiểm tra tính hợp lệ của Role
        var role = req.Role?.Trim().ToLowerInvariant() ?? "customer";
        if (role != "customer" && role != "driver_express" && role != "driver_truck" && role != "driver_container")
        {
            throw new AppException(AuthError.InvalidRole);
        }

        // 2. Tài xế bắt buộc phải có biển số xe
        if (role.StartsWith("driver") && string.IsNullOrWhiteSpace(req.LicensePlate))
        {
            throw new AppException(AuthError.DriverLicenseRequired);
        }

        // 3. Kiểm tra trùng lặp email
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            throw new AppException(AuthError.EmailAlreadyExists);
        }

        // 4. Khởi tạo thực thể User mới
        var u = new User
        {
            Name = req.Name.Trim(),
            Email = email,
            Role = role,
            Phone = req.Phone?.Trim() ?? "",
            LicensePlate = req.LicensePlate?.Trim() ?? ""
        };

        // 5. Hash mật khẩu và lưu vào database
        u.PasswordHash = _hasher.HashPassword(u, req.Password);
        _db.Users.Add(u);
        await _db.SaveChangesAsync();

        // 6. Trả về thông tin kết quả gọn gàng
        return new
        {
            message = "Đăng ký tài khoản thành công.",
            user_id = u.Id
        };
    }

    /// <summary>
    /// Xử lý nghiệp vụ đăng nhập hệ thống, kiểm tra mật khẩu, trạng thái khóa 
    /// và trả về chuỗi Token xác thực. Ném AppException nếu thông tin sai.
    /// </summary>
    public async Task<object> LoginAsync(LoginRequest req)
    {
        var email = req.Email?.Trim().ToLowerInvariant();
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);

        // 1. Kiểm tra user có tồn tại không & Xác thực mật khẩu có khớp không
        // (Lưu ý: Bạn cần chắc chắn trong class AuthService đã inject _hasher loại PasswordHasher<User>)
        if (u == null || string.IsNullOrEmpty(u.PasswordHash) ||
            _hasher.VerifyHashedPassword(u, u.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        {
            throw new AppException(AuthError.InvalidCredentials);
        }
        // 2. Kiểm tra xem tài khoản có đang bị vô hiệu hóa / khóa hay không
        if (!u.IsActive)
        {
            throw new AppException(AuthError.AccountLocked(u.BanReason));
        }

        // 3. Tiến hành sinh mã Access Token và Refresh Token
        var accessToken = GenerateJwtToken(u);
        var refreshToken = GenerateRefreshToken(u);

        // 4. Đóng gói kết quả trả về cho client
        return new
        {
            user_id = u.Id,
            name = u.Name,
            email = u.Email,
            phone = u.Phone,
            license_plate = u.LicensePlate,
            role = u.Role,
            balance = u.Balance,
            access_token = accessToken,
            refresh_token = refreshToken,
            token_type = "bearer"
        };
    }

    /// <summary>
    /// Hàm nội bộ: Tạo JSON Web Token (JWT) dùng làm Access Token (thời hạn 7 ngày).
    /// </summary>
    private string GenerateJwtToken(User u)
    {
        var secret = _config["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? "";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim("id", u.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.Name),
            new Claim(ClaimTypes.Email, u.Email),
            new Claim(ClaimTypes.Role, u.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Hàm nội bộ: Tạo Refresh Token riêng biệt với thời hạn dài hơn (30 ngày).
    /// </summary>
    private string GenerateRefreshToken(User u)
    {
        var secret = _config["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? "";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim("token_type", "refresh")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}