# Sơ đồ lớp tầng Domain (Domain Class Diagram)

Sơ đồ thể hiện các thực thể (Entity), đối tượng giá trị (Value Object), kiểu liệt kê (Enum) và quy tắc quan hệ trong tầng `FoodHub.Domain`. Tầng Domain độc lập hoàn toàn, không phụ thuộc vào bất kỳ tầng nào khác.

```mermaid
classDiagram
    class User {
        +Guid Id
        +string Email
        +string PasswordHash
        +string FullName
        +Role Role
        +Create(email, passwordHash, fullName, role)$ User
    }

    class Restaurant {
        +Guid Id
        +Guid OwnerId
        +string Name
        +string Address
        +IReadOnlyCollection~MenuItem~ MenuItems
        +Create(ownerId, name, address)$ Restaurant
        +AddMenuItem(name, price) MenuItem
    }

    class MenuItem {
        +Guid Id
        +Guid RestaurantId
        +string Name
        +Money Price
        +bool IsAvailable
        +Create(restaurantId, name, price)$ MenuItem
        +SetAvailability(isAvailable) void
        +ChangePrice(price) void
    }

    class Order {
        +Guid Id
        +Guid CustomerId
        +Guid RestaurantId
        +OrderStatus Status
        +DateTime CreatedAtUtc
        +string CouponCode
        +Money DiscountAmount
        +IReadOnlyCollection~OrderItem~ Items
        +Money Total
        +Money FinalAmount
        +Create(customerId, restaurantId, nowUtc)$ Order
        +AddItem(menuItem, quantity) void
        +ApplyCoupon(coupon, nowUtc) void
        +RemoveCoupon() void
        +Confirm() void
        +StartPreparing() void
        +MarkReadyForPickup() void
        +StartDelivering() void
        +MarkDelivered() void
        +Cancel() void
    }

    class OrderItem {
        +Guid Id
        +Guid OrderId
        +Guid MenuItemId
        +string Name
        +Money UnitPrice
        +int Quantity
        +Money LineTotal
        +Create(orderId, menuItem, quantity)$ OrderItem
        +IncreaseQuantity(quantity) void
    }

    class ShipperProfile {
        +Guid Id
        +Guid UserId
        +string VehiclePlate
        +IReadOnlyCollection~Delivery~ Deliveries
        +int ActiveDeliveryCount
        +Create(userId, vehiclePlate)$ ShipperProfile
        +AcceptOrder(orderId, nowUtc) Delivery
    }

    class Delivery {
        +Guid Id
        +Guid OrderId
        +Guid ShipperProfileId
        +DateTime AcceptedAtUtc
        +DateTime? StartedAtUtc
        +DateTime? DeliveredAtUtc
        +DeliveryCode Code
        +string CustomerCodeCipher
        +bool IsCompleted
        +Create(orderId, shipperProfileId, nowUtc)$ Delivery
        +StartDelivering(plainCode, nowUtc, customerCodeCipher) void
        +ConfirmReceipt(inputCode, nowUtc) CodeVerificationResult
    }

    class Review {
        +Guid Id
        +Guid OrderId
        +Guid CustomerId
        +Guid RestaurantId
        +int Rating
        +string Comment
        +DateTime CreatedAtUtc
        +Create(order, customerId, rating, comment, nowUtc)$ Review
    }

    class Coupon {
        +Guid Id
        +string Code
        +Money DiscountAmount
        +Money MinOrderAmount
        +DateTime ExpiresAtUtc
        +bool IsActive
        +Create(code, discountAmount, minOrderAmount, expiresAtUtc)$ Coupon
        +CalculateDiscount(orderSubtotal, nowUtc) Money
        +Deactivate() void
    }

    class RefreshToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTime ExpiresAtUtc
        +DateTime? RevokedAtUtc
        +Create(userId, tokenHash, expiresAtUtc)$ RefreshToken
        +IsActive(nowUtc) bool
        +Revoke(nowUtc) void
    }

    class Money {
        <<ValueObject>>
        +decimal Amount
        +Zero$ Money
        +Of(amount)$ Money
        +Add(other) Money
        +Multiply(quantity) Money
    }

    class DeliveryCode {
        <<ValueObject>>
        +string Salt
        +string Hash
        +DateTime ExpiresAtUtc
        +int FailedAttempts
        +bool IsUsed
        +bool IsLocked
        +Create(plainCode, nowUtc)$ DeliveryCode
        +Verify(input, nowUtc) CodeVerificationResult
    }

    class Role {
        <<Enumeration>>
        Admin
        Customer
        Restaurant
        Shipper
    }

    class OrderStatus {
        <<Enumeration>>
        Pending
        Confirmed
        Preparing
        ReadyForPickup
        Delivering
        Delivered
        Cancelled
    }

    class CodeVerificationResult {
        <<Enumeration>>
        Success
        Wrong
        Expired
        Locked
        AlreadyUsed
    }

    User --> Role
    Restaurant --> User : "OwnerId"
    Restaurant *-- MenuItem : "has"
    Order --> Restaurant : "RestaurantId"
    Order --> User : "CustomerId"
    Order --> OrderStatus
    Order *-- OrderItem : "contains"
    OrderItem --> MenuItem : "MenuItemId"
    OrderItem --> Money : "UnitPrice, LineTotal"
    Order --> Money : "DiscountAmount, Total, FinalAmount"
    MenuItem --> Money : "Price"
    ShipperProfile --> User : "UserId"
    ShipperProfile *-- Delivery : "manages"
    Delivery --> Order : "OrderId"
    Delivery *-- DeliveryCode : "Code (owns)"
    DeliveryCode ..> CodeVerificationResult
    Review --> Order : "OrderId"
    Review --> User : "CustomerId"
    Review --> Restaurant : "RestaurantId"
    Coupon --> Money : "DiscountAmount, MinOrderAmount"
    RefreshToken --> User : "UserId"
```
