# Blockers và việc cần làm lại

## Docker chưa chạy thử được

Máy phát triển hiện không có Docker. Vì vậy `Dockerfile` và `docker-compose.yml` mới chỉ được viết, chưa từng chạy. Kiểm thử tích hợp của repo dùng EF Core InMemory hoặc SQLite, không dùng Testcontainers và không gọi docker.

## Việc cần làm lại khi có Docker

1. Chạy `cp .env.example .env`, điền giá trị thật, rồi chạy `docker compose up --build` và xác nhận hai container `api` và `db` đều khởi động.
2. Mở http://localhost:8080/swagger, gọi thử `/health`, đăng nhập lấy token và thử nút Authorize với JWT Bearer.
3. Xác nhận migration EF Core chạy được trên PostgreSQL và dữ liệu mẫu được seed.
4. Chạy lại toàn bộ test với PostgreSQL thật nếu nhóm muốn so sánh với kết quả InMemory hoặc SQLite.
