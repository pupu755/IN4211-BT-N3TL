# Tổng hợp kết quả kiểm thử và độ bao phủ mã nguồn (Test Summary & Coverage)

Hệ thống được kiểm thử tự động toàn diện bằng **xUnit**, **Coverlet** và công cụ đo lường **ReportGenerator**. Tất cả các ca kiểm thử chạy độc lập hoàn toàn trên SQLite in-memory, không phụ thuộc vào hạ tầng Docker bên ngoài.

## 1. Tổng quan số liệu kiểm thử thật

- **Tổng số ca kiểm thử:** 137 ca.
- **Số ca kiểm thử đạt (Passed):** 137 ca (100%).
- **Số ca kiểm thử thất bại (Failed):** 0 ca.
- **Số ca kiểm thử bỏ qua (Skipped):** 0 ca.
- **Thời gian thực thi trung bình:** ~1 giây trên môi trường máy chủ CI / Local.
- **Độ bao phủ dòng mã nguồn (Line Coverage toàn hệ thống):** **92.6%** (1.179 / 1.272 dòng lệnh kiểm thử được, đã loại trừ mã sinh tự động từ migration EF Core).
- **Độ bao phủ phương thức (Method Coverage):** **94.6%** (337 / 356 phương thức).
- **Độ bao phủ nhánh điều kiện (Branch Coverage):** **68.5%** (198 / 289 nhánh rẽ).

### Bảng độ bao phủ theo từng tầng Clean Architecture

| Tầng dự án | Độ bao phủ dòng lệnh | Số dòng kiểm thử được | Số dòng được bao phủ |
|---|---|---|---|
| `FoodHub.Domain` | **95.9%** | 222 | 213 |
| `FoodHub.Application` | **93.0%** | 487 | 453 |
| `FoodHub.Infrastructure` | **92.9%** | 338 | 314 |
| `FoodHub.API` | **84.9%** | 225 | 191 |
| **Toàn bộ hệ thống** | **92.6%** | **1.272** | **1.179** |

---

## 2. Bảng kiểm thử theo quy tắc nghiệp vụ cốt lõi (Core Business Rules)

| Mã QT | Nội dung quy tắc nghiệp vụ | Tên ca kiểm thử chính (Test Method) | Số ca | Kết quả | Cài đặt tại lớp |
|---|---|---|---|---|---|
| **QT01** | Mỗi đơn chỉ gồm món của một quán ăn | `AddItem_FromDifferentRestaurant_ThrowsDomainException`<br>`AddItem_FromSameRestaurant_AddsItems`<br>`CreateOrder_ItemsFromTwoRestaurants_ThrowsDomainException` | 3 | Đạt | `FoodHub.Domain.Entities.Order`<br>`FoodHub.Application.Services.OrderService` |
| **QT02** | Shipper nhận tối đa 2 đơn chưa giao cùng lúc | `AcceptOrder_FirstOrder_CreatesActiveDelivery`<br>`AcceptOrder_SecondOrder_IsAllowed`<br>`AcceptOrder_ThirdUndeliveredOrder_ThrowsDomainException`<br>`AcceptOrder_AfterOneDelivered_IsAllowed`<br>`Accept_ThirdUndeliveredOrder_ThrowsDomainException` | 5 | Đạt | `FoodHub.Domain.Entities.ShipperProfile`<br>`FoodHub.Application.Services.DeliveryService` |
| **QT03** | Mã 6 số xác nhận nhận hàng, hash SHA-256, hạn 2 giờ, khóa sau 5 lần sai | `Create_InvalidFormat_ThrowsDomainException`<br>`Create_ValidCode_StoresHashNotPlainText`<br>`Create_SamePlainCodeTwice_ProducesDifferentHashes`<br>`Create_ValidCode_ExpiresAfterTwoHours`<br>`Verify_CorrectCode_ReturnsSuccess`<br>`Verify_CorrectCodeTwice_SecondCallReturnsAlreadyUsed`<br>`Verify_WrongCode_ReturnsWrongAndCountsAttempt`<br>`Verify_AfterExpiry_ReturnsExpired`<br>`Verify_ExactlyAtExpiry_StillAccepted`<br>`Verify_FiveWrongAttempts_LocksCode`<br>`Verify_CorrectCodeAfterLock_ReturnsLocked`<br>`Verify_FourWrongThenCorrect_ReturnsSuccess`<br>`StartDelivering_FirstTime_CreatesCode`<br>`StartDelivering_Twice_ThrowsDomainException`<br>`ConfirmReceipt_BeforeStart_ThrowsDomainException`<br>`ConfirmReceipt_CorrectCode_CompletesDelivery`<br>`ConfirmReceipt_WrongCode_DoesNotComplete`<br>`ConfirmReceipt_AfterCompleted_ReturnsAlreadyUsed`<br>`Protect_ThenUnprotect_ReturnsOriginalCode`<br>`Unprotect_TamperedCipher_Throws`<br>`Create_ManyCodes_AreAlwaysSixDigits`<br>`Validate_ConfirmReceiptCode_ChecksSixDigits`<br>`FullDeliveryFlow_WithCorrectCode_EndsDelivered`<br>`GetCode_ByAnotherCustomer_ThrowsForbidden`<br>`ConfirmReceipt_WrongCodeFiveTimes_LocksAndRejectsCorrectCode`<br>`FullOrderAndDeliveryFlow_ViaHttp_CompletesSuccessfully` | 30 | Đạt | `FoodHub.Domain.ValueObjects.DeliveryCode`<br>`FoodHub.Domain.Entities.Delivery`<br>`FoodHub.Infrastructure.Security.AesGcmCodeProtector`<br>`FoodHub.Infrastructure.Security.RandomDeliveryCodeFactory`<br>`FoodHub.Application.Services.DeliveryService` |
| **QT04** | Trạng thái đơn đi đúng thứ tự; chỉ được hủy trước Preparing | `Create_NewOrder_StartsPending`<br>`Confirm_EmptyOrder_ThrowsDomainException`<br>`Advance_FullHappyPath_ReachesDelivered`<br>`StartPreparing_FromPending_ThrowsDomainException`<br>`StartDelivering_FromPreparing_ThrowsDomainException`<br>`MarkDelivered_FromConfirmed_ThrowsDomainException`<br>`Confirm_AlreadyConfirmed_ThrowsDomainException`<br>`Cancel_FromPending_SetsCancelled`<br>`Cancel_FromConfirmed_SetsCancelled`<br>`Cancel_FromPreparing_ThrowsDomainException`<br>`Cancel_FromDelivering_ThrowsDomainException`<br>`Advance_AfterCancelled_ThrowsDomainException`<br>`StartPreparing_BeforeConfirm_ThrowsDomainException`<br>`Cancel_AfterPreparing_ThrowsDomainException`<br>`CancelOrder_ViaHttp_CustomerCancelsPending` | 15 | Đạt | `FoodHub.Domain.Entities.Order`<br>`FoodHub.Application.Services.OrderService` |
| **QT05** | Tiền dùng Money (decimal không âm); tổng đơn = tổng (giá × số lượng) | `Of_NegativeAmount_ThrowsDomainException`<br>`Of_Zero_ReturnsZeroAmount`<br>`Of_ValueWithManyDecimals_RoundsToTwoDecimals`<br>`Add_TwoAmounts_ReturnsSum`<br>`Multiply_ByQuantity_ReturnsProduct`<br>`Multiply_ByNegativeQuantity_ThrowsDomainException`<br>`Total_MultipleItems_EqualsSumOfPriceTimesQuantity`<br>`Total_EmptyOrder_IsZero`<br>`Total_AfterMenuPriceChange_KeepsPriceAtOrderTime`<br>`Validate_CreateOrderWithZeroQuantity_Fails`<br>`Validate_AddMenuItemWithNegativePrice_Fails`<br>`SaveOrder_WithItems_ReloadsWithSameTotalAndStatus`<br>`CreateOrder_ReturnsTotalFeeAndGrandTotal` | 13 | Đạt | `FoodHub.Domain.ValueObjects.Money`<br>`FoodHub.Domain.Entities.Order`<br>`FoodHub.Domain.Entities.OrderItem`<br>`FoodHub.Application.Services.OrderService` |

---

## 3. Bảng kiểm thử các chức năng mở rộng (Extension Features)

| Chức năng mở rộng | Thành viên phụ trách | Tên ca kiểm thử chính | Số ca | Kết quả | Cài đặt tại lớp |
|---|---|---|---|---|---|
| **1. Đánh giá quán sau khi đơn đã giao** | Bùi Trung Tín | `Create_DeliveredOrder_CreatesReviewSuccessfully`<br>`Create_NonDeliveredOrder_ThrowsDomainException`<br>`Create_ByDifferentCustomer_ThrowsDomainException`<br>`Create_InvalidRating_ThrowsDomainException`<br>`Create_CommentExceeds500Chars_ThrowsDomainException`<br>`Review_CreateForDeliveredOrder_Succeeds`<br>`Review_CreateTwiceForSameOrder_ThrowsConflict`<br>`GetReviews_Anonymous_ReturnsOk` | 9 | Đạt | `FoodHub.Domain.Entities.Review`<br>`FoodHub.Application.Services.ReviewService`<br>`FoodHub.API.Controllers.ReviewsController` |
| **2. Mã giảm giá (Coupon) áp dụng cho đơn** | Thái Văn Toàn | `Create_ValidCoupon_Succeeds`<br>`CalculateDiscount_SubtotalMeetsMin_ReturnsDiscountAmount`<br>`CalculateDiscount_SubtotalBelowMin_ThrowsDomainException`<br>`CalculateDiscount_Expired_ThrowsDomainException`<br>`CalculateDiscount_Inactive_ThrowsDomainException`<br>`ApplyCoupon_ToPendingOrder_SetsDiscountAndCode`<br>`RemoveCoupon_ResetsDiscount`<br>`Coupon_CreateAndApply_ReducesGrandTotal`<br>`Coupon_CreateDuplicateCode_ThrowsConflict`<br>`CreateCoupon_Admin_ReturnsOk`<br>`CreateCoupon_CustomerRole_ReturnsForbidden` | 11 | Đạt | `FoodHub.Domain.Entities.Coupon`<br>`FoodHub.Application.Services.CouponService`<br>`FoodHub.API.Controllers.CouponsController` |
| **3. Thống kê doanh thu cho chủ quán** | Võ Văn Toàn | `RevenueStatistics_DeliveredOrders_CalculatesCorrectTotals`<br>`RevenueStatistics_ByStranger_ThrowsForbidden`<br>`GetRestaurantStatistics_Owner_ReturnsOk`<br>`GetRestaurantStatistics_Customer_ReturnsForbidden` | 4 | Đạt | `FoodHub.Application.Services.RestaurantService`<br>`FoodHub.API.Controllers.RestaurantsController` |

---

## 4. Bảng kiểm thử bảo mật, phân quyền và kiến trúc Clean Architecture

| Nhóm kiểm thử | Tên ca kiểm thử chính | Số ca | Kết quả | Ghi chú |
|---|---|---|---|---|
| **Kiểm tra kiến trúc (Layer Dependency)** | `Domain_Assembly_DoesNotReferenceOuterLayers`<br>`Application_Assembly_DoesNotReferenceOuterLayers`<br>`Infrastructure_Assembly_DoesNotReferenceApi` | 3 | Đạt | Đảm bảo nguyên tắc Clean Architecture (phụ thuộc một chiều đi vào tâm) |
| **Bảo mật và mật khẩu (Security & Hash)** | `Hash_ThenVerify_WithSamePassword_ReturnsTrue`<br>`Verify_WithWrongPassword_ReturnsFalse`<br>`Hash_SamePasswordTwice_ProducesDifferentHashes`<br>`CreateAccessToken_ContainsUserIdAndRoleClaims`<br>`CreateRefreshToken_ReturnsPlainAndMatchingHash`<br>`SaveUser_DuplicateEmail_ViolatesUniqueIndex` | 6 | Đạt | PBKDF2 100.000 vòng lặp kèm salt ngẫu nhiên, JWT HMAC-SHA256 |
| **Quy trình xác thực & Refresh Token** | `Register_DuplicateEmail_ThrowsConflict`<br>`Register_AdminRole_ThrowsForbidden`<br>`Login_WrongPassword_ThrowsUnauthorized`<br>`Refresh_UsedTwice_SecondCallIsRejected` | 4 | Đạt | Chống tấn công giả mạo token và xoay vòng refresh token an toàn |
| **Mẫu tính phí (Delivery Fee Strategy)** | `Calculate_FlatStrategy_ReturnsSameFeeForAnySubtotal`<br>`Calculate_BelowThreshold_ChargesFallbackFee`<br>`Calculate_AtThreshold_IsFree`<br>`Calculate_DefaultStrategy_UsesDefaults` | 4 | Đạt | Mẫu Strategy tính phí giao hàng linh hoạt |
| **Xác thực dữ liệu đầu vào (FluentValidation)** | `Validate_RegisterWithShortPassword_Fails`<br>`Validate_RegisterShipperWithoutPlate_Fails`<br>`Validate_RegisterValidCustomer_Passes`<br>`Validate_CreateOrderWithoutItems_Fails`<br>`IsActive_BeforeExpiry_ReturnsTrue`<br>`IsActive_AfterExpiry_ReturnsFalse`<br>`IsActive_AfterRevoke_ReturnsFalse` | 7 | Đạt | Trả mã lỗi HTTP 422 Unprocessable Entity kèm chi tiết từng trường |
| **Kiểm thử tích hợp API qua HTTP (In-Memory)** | `Health_ReturnsOk`<br>`SwaggerJson_DeclaresBearerSecurityScheme`<br>`ProtectedEndpoint_WithoutToken_Returns401Json`<br>`CustomerEndpoint_WithShipperToken_Returns403Json`<br>`Login_SeededAccountOfEachRole_ReturnsToken` (4 tài khoản)<br>`Login_WrongPassword_Returns401`<br>`Register_InvalidBody_Returns422WithErrors`<br>`Register_ExistingEmail_Returns409`<br>`GetRestaurant_UnknownId_Returns404`<br>`ListRestaurants_Anonymous_ReturnsSeededRestaurant` | 13 | Đạt | Kiểm thử trọn vẹn pipeline HTTP, Middleware lỗi và Swagger |
