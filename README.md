````markdown
# Logistics API - Backend (ASP.NET Core 8 + SignalR)

Đây là source code Backend cho dự án Logistics, được xây dựng trên nền tảng **ASP.NET Core 8**, sử dụng **Entity Framework Core (SQLite)**, **JWT Authentication** và **SignalR** cho các tính năng Real-time.

## 🛠 Yêu cầu hệ thống (Prerequisites)

Trước khi chạy dự án, hãy đảm bảo máy tính của bạn đã cài đặt:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Bắt buộc)
- [Git](https://git-scm.com/) (Để clone source code)
- (Tùy chọn) [Visual Studio 2022](https://visualstudio.microsoft.com/) (Phiên bản 17.8 trở lên để hỗ trợ .NET 8)

_Lưu ý: Dự án đã được cấu hình sẵn `JWT_SECRET` và Database trong file `appsettings.json`, bạn không cần thiết lập thêm biến môi trường nào._

---

## 🚀 Cách 1: Chạy bằng Command Line (Terminal / CMD)

Cách này dành cho những ai muốn chạy nhanh mà không cần mở IDE.

**Bước 1:** Mở Terminal (Command Prompt, PowerShell, hoặc Git Bash).

**Bước 2:** Clone source code và di chuyển vào thư mục dự án:

```bash
git clone <URL_REPO_CUA_BAN>
cd
```
````

**Bước 3:** Chạy dự án bằng lệnh:

```bash
dotnet run
```

_Hệ thống sẽ tự động tải các gói NuGet cần thiết, tự động tạo database SQLite (`logistics-week8.db`), và khởi chạy server tại địa chỉ `http://localhost:5000`._

---

## 💻 Cách 2: Chạy bằng Visual Studio 2022

Cách này dành cho việc phát triển (Dev), có hỗ trợ debug và breakpoint.

**Bước 1:** Mở Visual Studio 2022.

**Bước 2:** Chọn **File** -> **Open** -> **Project/Solution...**

**Bước 3:** Tìm đến thư mục vừa clone và chọn file Solution:
👉 `Logistics.Api.slnx`

**Bước 4:** Bấm nút **Start** (hình tam giác màu xanh lá) hoặc nhấn **F5** để chạy dự án.
_Trình duyệt sẽ tự động mở trang Swagger UI để bạn test API._

---

## 📚 Các đường dẫn hữu ích sau khi chạy

- **Swagger API UI:** [http://localhost:5000/swagger](http://localhost:5000/swagger) (Chỉ hiển thị ở môi trường Development).
- **SignalR Hub:** `ws://localhost:5000/hubs/logistics` (Dành cho client kết nối real-time).
- **Health Check:** [http://localhost:5000/health](http://localhost:5000/health) (Kiểm tra server có đang chạy không).

## 🔐 Tài khoản Seed Data (Môi trường Dev)

Khi chạy lần đầu, hệ thống sẽ tự động tạo sẵn một số tài khoản để test:

- **Admin:** `admin@gmail.com` / `123123`
- **Customer:** `customer@gmail.com` / `123123`
- **Driver:** `driver@gmail.com` / `123123`

```

---
```
