# Các mẫu thiết kế và nguyên lý đã áp dụng (Design Patterns & Principles)

Dự án áp dụng các nguyên lý lập trình hướng đối tượng (OOP), các nguyên tắc **SOLID** và các mẫu thiết kế kinh điển phù hợp với yêu cầu thực tế của đề tài FoodHub.

## 1. Bảng tổng hợp các mẫu thiết kế

| Mẫu thiết kế | Nằm ở lớp / tệp mã nguồn | Vai trò và lý do nhóm chọn áp dụng |
|---|---|---|
| **Strategy** | `FoodHub.Application.Interfaces.IDeliveryFeeStrategy`<br>`FoodHub.Application.Strategies.FlatDeliveryFeeStrategy`<br>`FoodHub.Application.Strategies.FreeShippingOverThresholdStrategy` | **Lý do chọn:** Chính sách tính phí giao hàng của các nền tảng giao đồ ăn thường xuyên thay đổi (ví dụ: tính đồng giá 15.000đ hoặc miễn phí vận chuyển cho đơn hàng từ 200.000đ trở lên). Áp dụng Strategy giúp cô lập thuật toán tính phí thành các lớp riêng, dễ dàng hoán đổi cấu hình qua Dependency Injection mà không phải sửa đổi mã nguồn xử lý đơn hàng trong `Order` hay `OrderService` (tuân thủ nguyên lý Open/Closed trong SOLID). |
| **Factory Method** | `FoodHub.Domain.Interfaces.IDeliveryCodeFactory`<br>`FoodHub.Infrastructure.Security.RandomDeliveryCodeFactory` | **Lý do chọn:** Tầng Domain chỉ khai báo nhu cầu cần sinh một chuỗi mã xác nhận 6 số (`IDeliveryCodeFactory`). Việc thực hiện sinh ngẫu nhiên bằng bộ sinh số ngẫu nhiên mật mã (`RandomNumberGenerator`) được đẩy sang tầng Infrastructure (`RandomDeliveryCodeFactory`). Nhờ đó, trong kiểm thử tự động có thể dễ dàng thay thế bộ sinh mã cố định để kiểm tra các ca biên mà không can thiệp vào logic của Domain hay Service. |
| **Value Object (DDD)** | `FoodHub.Domain.ValueObjects.Money`<br>`FoodHub.Domain.ValueObjects.DeliveryCode` | **Lý do chọn:** Đề bài yêu cầu số tiền không được âm và mã 6 số phải có tính bảo mật cao (chỉ lưu hash, tự khóa sau 5 lần sai). Mẫu Value Object giúp gom cụm dữ liệu và các quy tắc kiểm tra tính hợp lệ thành các kiểu dữ liệu bất biến (immutable), tránh việc rải rác các hàm kiểm tra thủ công dạng `if (amount < 0)` ở nhiều nơi trong mã nguồn. |
| **Repository & Unit of Work** | `FoodHub.Application.Interfaces.IUnitOfWork`<br>`FoodHub.Application.Interfaces.IOrderRepository`<br>`FoodHub.Infrastructure.Repositories.*` | **Lý do chọn:** Trừu tượng hóa hoàn toàn tầng lưu trữ dữ liệu. Tầng Application chỉ làm việc với Interface đại diện cho danh mục thực thể, không phụ thuộc vào Entity Framework Core hay PostgreSQL. Điều này giúp nhóm dễ dàng chạy kiểm thử tích hợp trên SQLite in-memory mà không cần dựng PostgreSQL hay Docker. |

---

## 2. Ứng dụng các nguyên lý SOLID trong hệ thống

1. **S - Single Responsibility Principle (Đơn trách nhiệm):**
   - Mỗi thực thể Domain chỉ quản lý trạng thái và bất biến của chính nó: `Order` quản lý chuyển đổi trạng thái và danh sách món; `DeliveryCode` quản lý kiểm tra mã và số lần nhập sai.
   - Các use case trong tầng Application tách biệt rõ ràng theo chức năng: `AuthService` quản lý xác thực, `RestaurantService` quản lý quán và món, `OrderService` quản lý quy trình đơn, `DeliveryService` quản lý việc giao nhận.
2. **O - Open/Closed Principle (Mở để mở rộng, đóng để sửa đổi):**
   - Việc bổ sung thêm chính sách tính phí vận chuyển mới (ví dụ tính phí theo cự ly kilômét) chỉ cần tạo lớp mới triển khai `IDeliveryFeeStrategy` mà không sửa dòng mã nào trong `OrderService` hay `Mapper`.
3. **L - Liskov Substitution Principle (Thay thế Liskov):**
   - Mọi lớp cài đặt `IDeliveryFeeStrategy` (`FlatDeliveryFeeStrategy`, `FreeShippingOverThresholdStrategy`) đều có thể thay thế hoàn hảo cho nhau tại nơi tiếp nhận interface mà không làm thay đổi tính đúng đắn của chương trình.
4. **I - Interface Segregation Principle (Phân tách giao diện):**
   - Các giao diện repository được chia nhỏ theo từng thực thể riêng biệt (`IUserRepository`, `IOrderRepository`, `IRestaurantRepository`, `IReviewRepository`, `ICouponRepository`), tránh tạo ra một generic repository khổng lồ chứa hàng chục phương thức không cần thiết.
5. **D - Dependency Inversion Principle (Đảo ngược phụ thuộc):**
   - Tầng cấp cao (`Application`, `Domain`) không phụ thuộc vào tầng cấp thấp (`Infrastructure`, `API`). Ngược lại, tầng `Infrastructure` phụ thuộc vào các interface được định nghĩa trong `Application` và `Domain`. Toàn bộ phụ thuộc chỉ trỏ hướng vào tâm theo đúng kiến trúc Clean Architecture.
