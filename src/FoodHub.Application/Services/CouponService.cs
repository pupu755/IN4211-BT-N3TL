using FluentValidation;
using FoodHub.Application.Common;
using FoodHub.Application.Dtos;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Application.Services;

public sealed class CouponService
{
    private readonly ICouponRepository _coupons;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateCouponRequest> _createValidator;

    public CouponService(
        ICouponRepository coupons,
        IUnitOfWork uow,
        IValidator<CreateCouponRequest> createValidator)
    {
        _coupons = coupons;
        _uow = uow;
        _createValidator = createValidator;
    }

    public async Task<CouponDto> CreateAsync(CreateCouponRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_createValidator, request, ct);

        var existing = await _coupons.GetByCodeAsync(request.Code.Trim().ToUpperInvariant(), ct);
        if (existing is not null)
        {
            throw new ConflictException("Coupon code already exists.");
        }

        var coupon = Coupon.Create(
            request.Code,
            Money.Of(request.DiscountAmount),
            Money.Of(request.MinOrderAmount),
            request.ExpiresAtUtc);

        await _coupons.AddAsync(coupon, ct);
        await _uow.SaveChangesAsync(ct);
        return coupon.ToDto();
    }

    public async Task<CouponDto> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var coupon = await _coupons.GetByCodeAsync(code.Trim().ToUpperInvariant(), ct)
            ?? throw new NotFoundException("Coupon not found.");
        return coupon.ToDto();
    }

    public async Task<IReadOnlyList<CouponDto>> ListActiveAsync(CancellationToken ct = default) =>
        (await _coupons.ListActiveAsync(ct)).Select(c => c.ToDto()).ToList();
}
