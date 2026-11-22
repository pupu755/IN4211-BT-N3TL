# FoodHub (ĐT18 - Giao đồ ăn)

[![CI](https://github.com/pupu755/IN4211-BT-N3TL/actions/workflows/ci.yml/badge.svg)](https://github.com/pupu755/IN4211-BT-N3TL/actions/workflows/ci.yml)

Bài tập lớn môn IN4211, đề tài ĐT18 FoodHub. Hệ thống cho khách đặt món từ quán ăn và shipper giao hàng, gồm bốn vai trò Admin, Customer, Restaurant, Shipper. Giảng viên: ThS. Nguyễn Tuấn Kiệt. Nhóm 3.

Công nghệ: ASP.NET Core 8 Web API, Clean Architecture 4 tầng, EF Core, PostgreSQL, JWT, Swagger, xUnit và coverlet.

> Trạng thái: mới ở bước 1 (khung solution). Nghiệp vụ, xác thực và dữ liệu mẫu sẽ được thêm ở các bước sau.

## Cấu trúc

```
src/FoodHub.Domain          Entity, value object, quy tắc nghiệp vụ (không phụ thuộc tầng nào)
src/FoodHub.Application     Use case, interface repository, DTO, validation
src/FoodHub.Infrastructure  EF Core, PostgreSQL, JWT, repository
src/FoodHub.API             Controller, Swagger, middleware, cấu hình
tests/FoodHub.Tests         xUnit và coverlet
```

Phụ thuộc chỉ đi vào trong: Domain ← Application ← Infrastructure ← API.

## Chạy bằng Docker

```
cp .env.example .env
docker compose up --build
```

Sau khi sửa `.env` (đặt mật khẩu và khóa JWT thật), API chạy tại http://localhost:8080.

- Swagger: http://localhost:8080/swagger
- Kiểm tra sống: http://localhost:8080/health

Trong Swagger, bấm nút Authorize và dán access token (không cần gõ chữ Bearer).

## Chạy local không dùng Docker

Cần .NET SDK 8 và một PostgreSQL đang chạy. Đặt các biến môi trường theo `.env.example` (PowerShell):

```
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=foodhub;Username=foodhub;Password=..."
$env:Jwt__Key = "..."
$env:Jwt__Issuer = "FoodHub"
$env:Jwt__Audience = "FoodHub.Clients"
dotnet run --project src/FoodHub.API
```

## Chạy test

```
dotnet test
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

GitHub Actions (`.github/workflows/ci.yml`) build, test, đo coverage và tải báo cáo `coverage-report` lên artifact.

## Tài khoản mẫu

Dữ liệu mẫu sẽ được seed ở bước 5. Các tài khoản dự kiến:

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@foodhub.local | [cần bổ sung khi seed] |
| Customer | customer@foodhub.local | [cần bổ sung khi seed] |
| Restaurant | restaurant@foodhub.local | [cần bổ sung khi seed] |
| Shipper | shipper@foodhub.local | [cần bổ sung khi seed] |

## Phân công

| Thành viên | MSSV | Phần chính |
|---|---|---|
| Bùi Trung Tín | 0024416184 | Order/OrderItem, QT01, QT04, QT05, chức năng mở rộng 1 (đánh giá quán) |
| Thái Văn Toàn | 0024419416 | Auth JWT/refresh, phân quyền, Docker, CI, chức năng mở rộng 2 (mã giảm giá) |
| Võ Văn Toàn | 0024417563 | Shipper (QT02), mã 6 số (QT03), Delivery, chức năng mở rộng 3 (thống kê doanh thu) |
