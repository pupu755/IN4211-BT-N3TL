# Danh sách giao diện lập trình ứng dụng (API Endpoints)

Hệ thống cung cấp 30 API endpoints tuân thủ RESTful, xác thực qua JWT Bearer token và phân quyền chặt chẽ theo 4 vai trò người dùng: Admin, Customer, Restaurant, Shipper.

| STT | Phương thức | Đường dẫn API | Chức năng | Vai trò cho phép |
|---|---|---|---|---|
| 1 | POST | `/api/auth/register` | Đăng ký tài khoản mới (Customer, Restaurant, Shipper) | Tất cả (Anonymous) |
| 2 | POST | `/api/auth/login` | Đăng nhập nhận JWT access token và refresh token | Tất cả (Anonymous) |
| 3 | POST | `/api/auth/refresh` | Cấp lại cặp token mới khi access token hết hạn | Tất cả (Anonymous) |
| 4 | GET | `/api/restaurants` | Xem danh sách các quán ăn và thực đơn | Tất cả (Anonymous) |
| 5 | GET | `/api/restaurants/{id}` | Xem thông tin chi tiết một quán ăn | Tất cả (Anonymous) |
| 6 | GET | `/api/restaurants/mine` | Xem danh sách các quán ăn do chủ quán sở hữu | Restaurant |
| 7 | POST | `/api/restaurants` | Tạo hồ sơ quán ăn mới | Restaurant |
| 8 | POST | `/api/restaurants/{id}/menu-items` | Thêm món ăn mới vào thực đơn quán | Restaurant |
| 9 | GET | `/api/restaurants/{id}/orders` | Xem danh sách tất cả đơn hàng gửi tới quán | Restaurant |
| 10 | GET | `/api/restaurants/{id}/statistics` | Thống kê doanh thu, số đơn và top món bán chạy (Mở rộng 3) | Restaurant |
| 11 | POST | `/api/orders` | Tạo đơn hàng mới từ một quán ăn (QT01, QT05) | Customer |
| 12 | GET | `/api/orders/mine` | Xem danh sách lịch sử đơn hàng của khách hàng | Customer |
| 13 | GET | `/api/orders/{id}` | Xem chi tiết thông tin đơn hàng | Customer (chủ đơn), Restaurant (chủ quán), Shipper (được gán), Admin |
| 14 | POST | `/api/orders/{id}/cancel` | Hủy đơn hàng (chỉ hủy được trước khi nấu, QT04) | Customer, Restaurant, Admin |
| 15 | POST | `/api/orders/{id}/confirm` | Quán xác nhận đơn (Pending sang Confirmed, QT04) | Restaurant |
| 16 | POST | `/api/orders/{id}/prepare` | Quán bắt đầu chế biến món (Confirmed sang Preparing, QT04) | Restaurant |
| 17 | POST | `/api/orders/{id}/ready` | Món đã nấu xong chờ shipper (Preparing sang ReadyForPickup, QT04) | Restaurant |
| 18 | GET | `/api/orders/{id}/delivery-code` | Khách lấy mã 6 số để đọc cho shipper khi nhận hàng (QT03) | Customer (chủ đơn) |
| 19 | POST | `/api/orders/{id}/apply-coupon` | Áp dụng mã giảm giá vào đơn hàng chờ xác nhận (Mở rộng 2) | Customer |
| 20 | POST | `/api/orders/{id}/remove-coupon` | Hủy áp dụng mã giảm giá khỏi đơn hàng (Mở rộng 2) | Customer |
| 21 | GET | `/api/deliveries/available` | Xem danh sách đơn đang chờ lấy hàng (ReadyForPickup) | Shipper |
| 22 | POST | `/api/deliveries/{orderId}/accept` | Shipper nhận giao đơn (tối đa 2 đơn chưa giao, QT02) | Shipper |
| 23 | POST | `/api/deliveries/{orderId}/start` | Shipper lấy món và bắt đầu đi giao, sinh mã 6 số (QT03, QT04) | Shipper |
| 24 | POST | `/api/deliveries/{orderId}/confirm-receipt` | Shipper nhập mã 6 số do khách đọc để hoàn tất giao hàng (QT03, QT04) | Shipper |
| 25 | POST | `/api/reviews/orders/{orderId}` | Khách hàng đánh giá quán cho đơn hàng đã giao (Mở rộng 1) | Customer |
| 26 | GET | `/api/reviews/restaurants/{restaurantId}` | Xem danh sách đánh giá của một quán ăn (Mở rộng 1) | Tất cả (Anonymous) |
| 27 | POST | `/api/coupons` | Tạo mã giảm giá mới trên hệ thống (Mở rộng 2) | Admin |
| 28 | GET | `/api/coupons` | Xem danh sách mã giảm giá còn hiệu lực (Mở rộng 2) | Đã đăng nhập (Authenticated) |
| 29 | GET | `/api/coupons/{code}` | Tra cứu chi tiết mã giảm giá theo mã nhập (Mở rộng 2) | Đã đăng nhập (Authenticated) |
| 30 | GET | `/health` | Kiểm tra tình trạng hoạt động của hệ thống Web API | Tất cả (Anonymous) |
