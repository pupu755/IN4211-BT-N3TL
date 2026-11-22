using FluentValidation;
using FoodHub.Application.Common;
using FoodHub.Application.Dtos;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.Interfaces;

namespace FoodHub.Application.Services;

public sealed class DeliveryService
{
    private readonly IOrderRepository _orders;
    private readonly IShipperRepository _shippers;
    private readonly IDeliveryRepository _deliveries;
    private readonly IDeliveryCodeFactory _codeFactory;
    private readonly ICodeProtector _protector;
    private readonly IDeliveryFeeStrategy _feeStrategy;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IValidator<ConfirmReceiptRequest> _receiptValidator;

    public DeliveryService(
        IOrderRepository orders,
        IShipperRepository shippers,
        IDeliveryRepository deliveries,
        IDeliveryCodeFactory codeFactory,
        ICodeProtector protector,
        IDeliveryFeeStrategy feeStrategy,
        IUnitOfWork uow,
        IClock clock,
        IValidator<ConfirmReceiptRequest> receiptValidator)
    {
        _orders = orders;
        _shippers = shippers;
        _deliveries = deliveries;
        _codeFactory = codeFactory;
        _protector = protector;
        _feeStrategy = feeStrategy;
        _uow = uow;
        _clock = clock;
        _receiptValidator = receiptValidator;
    }

    public async Task<IReadOnlyList<OrderDto>> ListAvailableAsync(CancellationToken ct = default) =>
        (await _orders.ListAvailableForDeliveryAsync(ct)).Select(o => o.ToDto(_feeStrategy)).ToList();

    /// <summary>A shipper accepts a ready order. QT02 is enforced inside ShipperProfile.</summary>
    public async Task<DeliveryDto> AcceptAsync(Guid shipperUserId, Guid orderId, CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(shipperUserId, ct);
        var order = await LoadOrderAsync(orderId, ct);

        if (order.Status != OrderStatus.ReadyForPickup)
        {
            throw new ConflictException("Only orders ready for pickup can be accepted.");
        }

        if (await _deliveries.GetByOrderIdAsync(orderId, ct) is not null)
        {
            throw new ConflictException("This order was already accepted by a shipper.");
        }

        var delivery = profile.AcceptOrder(orderId, _clock.UtcNow);
        await _uow.SaveChangesAsync(ct);
        return delivery.ToDto();
    }

    /// <summary>The shipper picks the food up. A six digit code is created for the customer (QT03).</summary>
    public async Task<DeliveryDto> StartDeliveringAsync(Guid shipperUserId, Guid orderId, CancellationToken ct = default)
    {
        var (delivery, order) = await LoadOwnDeliveryAsync(shipperUserId, orderId, ct);

        var plain = _codeFactory.Create();
        order.StartDelivering();
        delivery.StartDelivering(plain, _clock.UtcNow, _protector.Protect(plain));
        await _uow.SaveChangesAsync(ct);
        return delivery.ToDto();
    }

    /// <summary>Only the customer of the order can read the code.</summary>
    public async Task<DeliveryCodeDto> GetCodeForCustomerAsync(Guid customerId, Guid orderId, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(orderId, ct);
        if (order.CustomerId != customerId)
        {
            throw new ForbiddenException("This order belongs to another customer.");
        }

        var delivery = await _deliveries.GetByOrderIdAsync(orderId, ct);
        if (delivery?.Code is null || delivery.CustomerCodeCipher is null)
        {
            throw new NotFoundException("No delivery code has been issued for this order yet.");
        }

        return new DeliveryCodeDto(
            _protector.Unprotect(delivery.CustomerCodeCipher),
            delivery.Code.ExpiresAtUtc,
            delivery.Code.IsUsed,
            delivery.Code.IsLocked);
    }

    /// <summary>The shipper enters the code the customer reads out. Failed attempts are saved before the error is raised.</summary>
    public async Task<DeliveryDto> ConfirmReceiptAsync(Guid shipperUserId, Guid orderId, ConfirmReceiptRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_receiptValidator, request, ct);
        var (delivery, order) = await LoadOwnDeliveryAsync(shipperUserId, orderId, ct);

        var result = delivery.ConfirmReceipt(request.Code, _clock.UtcNow);
        if (result == CodeVerificationResult.Success)
        {
            order.MarkDelivered();
            await _uow.SaveChangesAsync(ct);
            return delivery.ToDto();
        }

        await _uow.SaveChangesAsync(ct);
        throw result switch
        {
            CodeVerificationResult.Wrong => new RequestValidationException(
                new Dictionary<string, string[]> { ["code"] = new[] { "The code is incorrect." } }),
            CodeVerificationResult.Expired => new ConflictException("The code has expired."),
            CodeVerificationResult.Locked => new ConflictException("The code is locked after too many wrong attempts."),
            _ => new ConflictException("The code was already used.")
        };
    }

    private async Task<ShipperProfile> LoadProfileAsync(Guid shipperUserId, CancellationToken ct) =>
        await _shippers.GetByUserIdAsync(shipperUserId, ct) ?? throw new NotFoundException("Shipper profile not found.");

    private async Task<Order> LoadOrderAsync(Guid orderId, CancellationToken ct) =>
        await _orders.GetByIdAsync(orderId, ct) ?? throw new NotFoundException("Order not found.");

    private async Task<(Delivery Delivery, Order Order)> LoadOwnDeliveryAsync(Guid shipperUserId, Guid orderId, CancellationToken ct)
    {
        var profile = await LoadProfileAsync(shipperUserId, ct);
        var order = await LoadOrderAsync(orderId, ct);
        var delivery = await _deliveries.GetByOrderIdAsync(orderId, ct)
            ?? throw new NotFoundException("Delivery not found.");
        if (delivery.ShipperProfileId != profile.Id)
        {
            throw new ForbiddenException("This delivery belongs to another shipper.");
        }

        return (delivery, order);
    }
}
