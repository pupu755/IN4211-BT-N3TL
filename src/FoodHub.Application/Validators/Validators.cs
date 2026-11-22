using FluentValidation;
using FoodHub.Application.Dtos;
using FoodHub.Domain.Enums;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.VehiclePlate)
            .NotEmpty().When(x => x.Role == Role.Shipper)
            .WithMessage("Vehicle plate is required for shippers.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class CreateRestaurantRequestValidator : AbstractValidator<CreateRestaurantRequest>
{
    public CreateRestaurantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddMenuItemRequestValidator : AbstractValidator<AddMenuItemRequest>
{
    public AddMenuItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.MenuItemId).NotEmpty();
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 100);
        });
    }
}

public sealed class ConfirmReceiptRequestValidator : AbstractValidator<ConfirmReceiptRequest>
{
    public ConfirmReceiptRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(DeliveryCode.CodeLength).Matches("^[0-9]+$");
    }
}
