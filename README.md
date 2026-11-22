# FoodHub (ĐT18 - Giao đồ ăn)

[![CI](https://github.com/pupu755/IN4211-BT-N3TL/actions/workflows/ci.yml/badge.svg)](https://github.com/pupu755/IN4211-BT-N3TL/actions/workflows/ci.yml)

Bài tập lớn môn IN4211, đề tài ĐT18 FoodHub. Hệ thống cho khách đặt món từ quán ăn và shipper giao hàng, gồm bốn vai trò Admin, Customer, Restaurant, Shipper. Giảng viên: ThS. Nguyễn Tuấn Kiệt. Nhóm 3.

Công nghệ: ASP.NET Core 8 Web API, Clean Architecture 4 tầng, EF Core, PostgreSQL, JWT, Swagger, xUnit và coverlet.

> Trạng thái: hoàn thành đầy đủ 4 tầng Clean Architecture, các quy tắc nghiệp vụ QT01–QT05, 3 chức năng mở rộng, 137 ca kiểm thử (92.6% coverage). Docker chưa được chạy thử trên máy phát triển, xem docs/BLOCKERS.md.

## Danh sách chức năng chính

1. **QT01:** Mỗi đơn chỉ gồm món của một quán.
2. **QT02:** Shipper nhận tối đa 2 đơn chưa giao cùng lúc.
3. **QT03:** Mã 6 số xác nhận nhận hàng sinh khi bắt đầu giao, có hạn 2 giờ, khóa sau 5 lần nhập sai, lưu dạng salted hash.
4. **QT04:** Trạng thái đơn đi đúng thứ tự: Pending → Confirmed → Preparing → ReadyForPickup → Delivering → Delivered. Chỉ được hủy trước Preparing.
5. **QT05:** Tiền dùng Money (decimal không âm); tổng đơn bằng tổng giá nhân số lượng.
6. **Mở rộng 1:** Đánh giá và nhận xét quán sau khi đơn giao thành công (Bùi Trung Tín).
7. **Mở rộng 2:** Áp dụng mã giảm giá (Coupon) theo ngưỡng đơn tối thiểu (Thái Văn Toàn).
8. **Mở rộng 3:** Thống kê doanh thu, số đơn và món bán chạy cho chủ quán (Võ Văn Toàn).

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
$env:Security__CodeKey = "..."
$env:Seed__Password = "..."   # tùy chọn, bật seed tài khoản mẫu
dotnet run --project src/FoodHub.API
```

## Chạy test

```
dotnet test
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

GitHub Actions (`.github/workflows/ci.yml`) build, test, đo coverage và tải báo cáo `coverage-report` lên artifact.

## Tài khoản mẫu

Khi API khởi động, migration được chạy và bốn tài khoản dưới đây được seed nếu biến `Seed__Password` có giá trị. Mật khẩu của cả bốn tài khoản chính là giá trị đó (`.env.example` đặt sẵn `Foodhub#123`, nhóm nên đổi). Seed còn tạo một quán mẫu `Sample Pho 24` có hai món.

| Vai trò | Email |
|---|---|
| Admin | admin@foodhub.local |
| Customer | customer@foodhub.local |
| Restaurant | restaurant@foodhub.local |
| Shipper | shipper@foodhub.local |

## Phân công

| Thành viên | MSSV | Phần chính |
|---|---|---|
| Bùi Trung Tín | 0024416184 | Order/OrderItem, QT01, QT04, QT05, chức năng mở rộng 1 (đánh giá quán) |
| Thái Văn Toàn | 0024419416 | Auth JWT/refresh, phân quyền, Docker, CI, chức năng mở rộng 2 (mã giảm giá) |
| Võ Văn Toàn | 0024417563 | Shipper (QT02), mã 6 số (QT03), Delivery, chức năng mở rộng 3 (thống kê doanh thu) |

