using Logistics.Api.Common.Filters;
using Logistics.Api.Data;
using Logistics.Api.Domain.Entities;
using Logistics.Api.Features.Auth;
using Logistics.Api.Features.Drivers;
using Logistics.Api.Features.Orders;
using Logistics.Api.Features.Users;
using Logistics.Api.Hubs;
using Logistics.Api.Middlewares; 
using Logistics.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using static Logistics.Api.Utils.LogisticsUtils;

namespace Logistics.Api;

public static class ApplicationSetup
{
    public static readonly string CorsPolicyName = "frontend";

    /// <summary>
    /// Đăng ký toàn bộ Services và cấu hình cho DI Container.
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // 1. Cấu hình Urls & kiểm tra JWT Secret
        var secret = builder.Configuration["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Hãy đặt JWT_SECRET >=32 bytes (PowerShell: $env:JWT_SECRET='your-long-unique-secret...').");

        builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000");

        // 2. Database & Identity Services
        builder.Services.AddDbContext<AppDbContext>(o =>
            o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
        builder.Services.AddSingleton<PasswordHasher<User>>();

        // 3. Business Services
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<OrderService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<DriverService>();
        builder.Services.AddScoped<AddressService>();
        builder.Services.AddScoped<DriverServiceAreaService>();
        builder.Services.AddScoped<DriverMatchingService>();
        builder.Services.AddScoped<DriverVehicleService>();
        builder.Services.AddScoped<DriverOrderService>();
        builder.Services.AddScoped<DriverStatisticsAndEarningsService>();
        builder.Services.AddScoped<DriverHistoryService>();

        // SignalR
        builder.Services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        });

        // 4. JWT Authentication & Authorization
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                o.Events = new JwtBearerEvents
                {
                    // SignalR gửi token qua query string "access_token" (không dùng Authorization header)
                    OnMessageReceived = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments(HubPaths.Logistics))
                        {
                            var token = ctx.Request.Query["access_token"];
                            if (!string.IsNullOrWhiteSpace(token)) ctx.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        // 5. CORS Policy
        builder.Services.AddCors(o => o.AddPolicy(CorsPolicyName, p =>
            p.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://127.0.0.1:5173")
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials()));

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

        // 6. Swagger API Explorer
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<ModelValidationFilter>();

        return builder;
    }

    /// <summary>
    /// Cấu hình Middleware Pipeline, Swagger, Custom Middleware và Đăng ký Endpoints.
    /// </summary>
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        // ============================================================
        // THỨ TỰ PIPELINE QUAN TRỌNG:
        //   UseCors → Logging → GlobalException → Auth → Custom → Authz
        //
        // - UseCors NGOÀI CÙNG: đảm bảo MỌI response (kể cả lỗi) đều có CORS header
        // - Logging: đo toàn bộ thời gian request
        // - GlobalException: bắt mọi exception từ pipeline bên trong
        // ============================================================

        // 1. CORS — phải NGOÀI CÙNG để thêm header vào cả response lỗi
        app.UseCors(CorsPolicyName);

        // 2. LoggingMiddleware — đo toàn bộ thời gian request
        app.UseMiddleware<LoggingMiddleware>();

        // 3. GlobalExceptionMiddleware — bắt toàn bộ ngoại lệ, trả ApiResponse chuẩn
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // 4. Authentication — parse JWT
        app.UseAuthentication();

        // 5. Middleware kiểm tra tài khoản bị vô hiệu hóa
        //    ⚠️ SKIP /hubs — tránh query DB mỗi lần SignalR negotiate/connect
        app.Use(async (context, next) =>
        {
            // Bỏ qua SignalR hub: JWT đã validate, user bị ban sẽ không login được
            if (context.Request.Path.StartsWithSegments("/hubs"))
            {
                await next();
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (!await db.Users.AnyAsync(u => u.Id == UserId(context.User) && u.IsActive))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { detail = "Tài khoản đã bị vô hiệu hóa." });
                    return;
                }
            }

            await next();
        });

        // 6. Authorization — check role/policy
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Health Check API Endpoint
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            technology = "ASP.NET Core 8 + SignalR + Clean Architecture",
            utc = DateTime.UtcNow
        }));

        // Mapping các Feature Endpoints & SignalR Hub
        app.MapAuthEndpoints();
        app.MapOrderEndpoints();
        app.MapUserEndpoints();
        app.MapAddressEndpoints();
        app.MapDriverEndpoints();
        app.MapDriverServiceAreaEndpoints();
        app.MapDriverOrderEndpoints();
        app.MapDriverVehicleEndpoints();
        app.MapDriverStatisticsAndEarningsEndpoints();
        app.MapDriverHistoryEndpoints();


        app.MapHub<LogisticsHub>(HubPaths.Logistics);

        return app;
    }

    /// <summary>
    /// Khởi tạo dữ liệu ban đầu (Seed Data) cho môi trường Development.
    /// </summary>
    public static async Task<WebApplication> SeedDataAsync(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (app.Environment.IsDevelopment())
            {
                var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
                var demoUsers = new[]
                {
                    ("admin@gmail.com", "Quản trị Demo", "ADMIN"),
                    ("customer@gmail.com", "Khách hàng Demo", "CUSTOMER"),
                    ("driver@gmail.com", "Tài xế Demo", "DRIVER_EXPRESS")
                };

                foreach (var (email, name, role) in demoUsers)
                {
                    if (await db.Users.AnyAsync(u => u.Email == email)) continue;

                    var user = new User
                    {
                        Name = name,
                        Email = email,
                        Role = role,
                        Phone = "0900000000",
                        LicensePlate = role.StartsWith("DRIVER") ? "59A-12345" : "",
                        Balance = role.StartsWith("DRIVER") ? 500000m : 0m
                    };
                    user.PasswordHash = hasher.HashPassword(user, "123123");
                    db.Users.Add(user);
                }

                await db.SaveChangesAsync();
            }
        }

        return app;
    }
}