# Bảng đối chiếu yêu cầu đề bài và kết quả thực hiện (Checklist)

Tài liệu này đối chiếu từng yêu cầu trong văn bản Đề tài bài tập lớn IN4211 của Khoa Công nghệ và Kỹ thuật và bản quy ước `AGENTS.md` với mã nguồn và kết quả thực thi thực tế trong kho lưu trữ GitHub: https://github.com/pupu755/IN4211-BT-N3TL.git

---

## 1. Yêu cầu nghiệp vụ đề tài ĐT18 (FoodHub – Giao đồ ăn)

| STT | Yêu cầu nghiệp vụ | Tình trạng | Bằng chứng mã nguồn / Ca kiểm thử |
|---|---|---|---|
| 1 | **QT01:** Mỗi đơn chỉ gồm món của một quán ăn | **ĐẠT** | - `src/FoodHub.Domain/Entities/Order.cs` (phương thức `AddItem`) kiểm tra `menuItem.RestaurantId != RestaurantId`<br>- Kiểm thử: `OrderTests.AddItem_FromDifferentRestaurant_ThrowsDomainException`, `UseCaseFlowTests.CreateOrder_ItemsFromTwoRestaurants_ThrowsDomainException` |
| 2 | **QT02:** Shipper nhận tối đa 2 đơn chưa giao cùng lúc | **ĐẠT** | - `src/FoodHub.Domain/Entities/ShipperProfile.cs` (`MaxActiveOrders = 2`, `ActiveDeliveryCount`)<br>- Kiểm thử: `ShipperProfileTests.AcceptOrder_ThirdUndeliveredOrder_ThrowsDomainException`, `UseCaseFlowTests.Accept_ThirdUndeliveredOrder_ThrowsDomainException` |
| 3 | **QT03:** Mã 6 số xác nhận giao hàng (sinh khi bắt đầu giao, có hạn 2 giờ, sai quá 5 lần thì khóa, lưu dạng hash) | **ĐẠT** | - `src/FoodHub.Domain/ValueObjects/DeliveryCode.cs`<br>- `src/FoodHub.Domain/Entities/Delivery.cs` (chỉ khách hàng sở hữu đơn mới đọc được mã đã giải mã AES-GCM qua `ICodeProtector`)<br>- Kiểm thử: `DeliveryCodeTests.Verify_FiveWrongAttempts_LocksCode`, `UseCaseFlowTests.ConfirmReceipt_WrongCodeFiveTimes_LocksAndRejectsCorrectCode`, `ApiSmokeTests.FullOrderAndDeliveryFlow_ViaHttp_CompletesSuccessfully` |
| 4 | **QT04:** Trạng thái đơn đi đúng thứ tự: Pending → Confirmed → Preparing → ReadyForPickup → Delivering → Delivered; chỉ được hủy trước Preparing | **ĐẠT** | - `src/FoodHub.Domain/Entities/Order.cs` (`Transition`, `Cancel`)<br>- Kiểm thử: `OrderTests.Advance_FullHappyPath_ReachesDelivered`, `OrderTests.Cancel_FromPreparing_ThrowsDomainException`, `ApiSmokeTests.CancelOrder_ViaHttp_CustomerCancelsPending` |
| 5 | **QT05:** Tiền dùng Money (decimal không âm); tổng đơn bằng tổng (giá × số lượng) | **ĐẠT** | - `src/FoodHub.Domain/ValueObjects/Money.cs`<br>- `src/FoodHub.Domain/Entities/Order.cs` (`Total`, `FinalAmount`), `OrderItem.cs` (`LineTotal`)<br>- Kiểm thử: `MoneyTests.Of_NegativeAmount_ThrowsDomainException`, `OrderTests.Total_MultipleItems_EqualsSumOfPriceTimesQuantity` |

---

## 2. Yêu cầu 3 chức năng mở rộng (Mỗi thành viên phụ trách 1 chức năng)

| STT | Chức năng mở rộng | Thành viên phụ trách | Tình trạng | Bằng chứng mã nguồn / Ca kiểm thử |
|---|---|---|---|---|
| 1 | **Đánh giá quán sau khi đơn đã giao:** Khách hàng chỉ được đánh giá (1–5 sao kèm nhận xét tối đa 500 ký tự) các đơn hàng ở trạng thái Delivered; mỗi đơn đánh giá 1 lần | Bùi Trung Tín | **ĐẠT** | - `src/FoodHub.Domain/Entities/Review.cs`<br>- `src/FoodHub.Application/Services/ReviewService.cs`<br>- `src/FoodHub.API/Controllers/Controllers.cs` (`ReviewsController`)<br>- Kiểm thử: `ReviewTests.cs`, `ExtensionServiceTests.Review_CreateForDeliveredOrder_Succeeds` |
| 2 | **Mã giảm giá (Coupon) áp dụng cho đơn:** Khách hàng áp dụng mã giảm giá khi đơn còn Pending; kiểm tra hạn dùng, trạng thái kích hoạt và giá trị đơn tối thiểu | Thái Văn Toàn | **ĐẠT** | - `src/FoodHub.Domain/Entities/Coupon.cs`<br>- `src/FoodHub.Application/Services/CouponService.cs`<br>- `src/FoodHub.API/Controllers/Controllers.cs` (`CouponsController`, `OrdersController.ApplyCoupon`)<br>- Kiểm thử: `CouponTests.cs`, `ExtensionServiceTests.Coupon_CreateAndApply_ReducesGrandTotal`, `ExtensionApiTests.CreateCoupon_Admin_ReturnsOk` |
| 3 | **Thống kê doanh thu cho chủ quán:** Thống kê tổng số đơn thành công, tổng doanh thu thực nhận, số đơn bị hủy và danh sách món ăn bán chạy nhất theo khoảng thời gian | Võ Văn Toàn | **ĐẠT** | - `src/FoodHub.Application/Services/RestaurantService.cs` (`GetRevenueStatisticsAsync`)<br>- `src/FoodHub.API/Controllers/Controllers.cs` (`RestaurantsController.GetStatistics`)<br>- Kiểm thử: `ExtensionServiceTests.RevenueStatistics_DeliveredOrders_CalculatesCorrectTotals`, `ExtensionApiTests.GetRestaurantStatistics_Owner_ReturnsOk` |

---

## 3. Yêu cầu công nghệ và kiến trúc hệ thống

| STT | Yêu cầu kỹ thuật | Tình trạng | Bằng chứng kiểm chứng |
|---|---|---|---|
| 1 | ASP.NET Core 8 Web API | **ĐẠT** | `src/FoodHub.API/FoodHub.API.csproj` (`net8.0`) |
| 2 | Clean Architecture 4 tầng (Domain, Application, Infrastructure, API) | **ĐẠT** | Các dự án trong `FoodHub.sln`; kiểm thử `LayerDependencyTests.cs` kiểm tra không phụ thuộc ngược |
| 3 | Entity Framework Core và PostgreSQL | **ĐẠT** | `Npgsql.EntityFrameworkCore.PostgreSQL`, các cấu hình trong `Configurations/`, hai migration `InitialCreate` và `AddExtensions` |
| 4 | Xác thực JWT Bearer và Refresh Token | **ĐẠT** | `JwtTokenService.cs`, `AuthController.cs`, bảng `RefreshTokens`, tự động thu hồi token cũ khi cấp lại |
| 5 | Swagger UI có nút Authorize (JWT Bearer) | **ĐẠT** | Cấu hình `AddSecurityDefinition("Bearer", ...)` trong `src/FoodHub.API/Program.cs` |
| 6 | Áp dụng ít nhất 2 mẫu thiết kế | **ĐẠT** | Strategy (`IDeliveryFeeStrategy`), Factory Method (`IDeliveryCodeFactory`), Value Object (`Money`, `DeliveryCode`), Repository & Unit of Work (xem `docs/patterns.md`) |
| 7 | Data Transfer Object (DTO) và Validation | **ĐẠT** | `Dtos.cs`, thư viện `FluentValidation` trong `Validators.cs`, tự động ném `RequestValidationException` |
| 8 | Middleware xử lý ngoại lệ chuẩn hóa JSON (401, 403, 404, 409, 422, 500) | **ĐẠT** | `src/FoodHub.API/Middleware/ExceptionHandlingMiddleware.cs`, kiểm thử `ApiSmokeTests.ProtectedEndpoint_WithoutToken_Returns401Json`, v.v. |
| 9 | Kiểm thử tự động xUnit và đo độ bao phủ | **ĐẠT** | **137 ca kiểm thử** đều đạt (0 fail, 0 skip). Độ bao phủ toàn hệ thống đạt **92.6%** (Domain: 95.9%, Application: 93.0%, Infrastructure: 92.9%, API: 84.9%) |
| 10 | Tích hợp liên tục GitHub Actions (CI) | **ĐẠT** | `.github/workflows/ci.yml` chạy tự động build, test, tính coverage và tải báo cáo lên artifact cho mỗi PR và push `main` |
| 11 | Không lưu lộ lọt mật khẩu, có file `.env.example` | **ĐẠT** | `.env.example` chứa toàn bộ biến môi trường mẫu, không có secret nhạy cảm nào được commit |
| 12 | Seed dữ liệu mẫu cho cả 4 vai trò | **ĐẠT** | `src/FoodHub.Infrastructure/Persistence/DbSeeder.cs` (Admin, Customer, Restaurant, Shipper kèm quán và món mẫu) |
| 13 | Quy trình Git: commit nhỏ, tạo nhánh feature và Pull Request | **ĐẠT** | Đã tạo và merge thành công 7 Pull Request (từ PR #1 đến PR #7) |

---

## 4. Các công việc cần thực hiện lại khi môi trường máy chủ có Docker

Do máy tính phát triển cục bộ hiện chưa cài đặt Docker Daemon, nhóm đã tuân thủ việc cô lập kiểm thử tích hợp trên bộ nhớ (SQLite in-memory) và cấu hình sẵn sàng hạ tầng Docker. Dưới đây là danh sách các việc cần chạy kiểm tra lại khi máy có Docker:

1. **Khởi chạy container:**
   ```bash
   cp .env.example .env
   # Chỉnh sửa mật khẩu bí mật trong file .env
   docker compose up --build
   ```
2. **Kiểm tra tự động Migration trên PostgreSQL:**
   - Khi container `api` khởi động, xác nhận EF Core tự động áp dụng 2 migration `InitialCreate` và `AddExtensions` vào cơ sở dữ liệu `foodhub` trong container `db` (PostgreSQL 16) mà không báo lỗi.
3. **Kiểm tra Seed dữ liệu trên PostgreSQL thật:**
   - Xác nhận bảng `Users`, `Restaurants`, `MenuItems`, `ShipperProfiles` có dữ liệu mẫu được sinh tự động thông qua `Seed__Password`.
4. **Kiểm tra giao diện Swagger trực tiếp:**
   - Mở trình duyệt tại http://localhost:8080/swagger.
   - Thử nghiệm gọi API đăng nhập `/api/auth/login` với tài khoản `admin@foodhub.local`.
   - Bấm nút xanh **Authorize**, dán chuỗi access token nhận được vào ô nhập (không cần chữ Bearer) và gọi thử các endpoint yêu cầu quyền Admin hoặc Customer.
