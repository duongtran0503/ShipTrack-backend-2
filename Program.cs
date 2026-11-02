using Logistics.Api;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình toàn bộ Services (DI Container)
builder.ConfigureServices();

var app = builder.Build();

// Cấu hình Middleware Pipeline & Routes
app.ConfigureMiddleware();

// Seed data khi ứng dụng khởi chạy
await app.SeedDataAsync();

app.Run();