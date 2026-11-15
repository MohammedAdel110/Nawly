using FluentValidation;
using Nawly.Domain.Models.Ordering;
using Nawly.Shared.DTOs.Cart;
using Nawly.Shared.DTOs.Order;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Services.Validators;

public sealed class UpsertCartItemDtoValidator : AbstractValidator<UpsertCartItemDto>
{
    public UpsertCartItemDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, Cart.MaxQuantityPerItem);
        RuleFor(x => x.Note).MaximumLength(300);
    }
}

public sealed class CartQuoteRequestDtoValidator : AbstractValidator<CartQuoteRequestDto>
{
    public CartQuoteRequestDtoValidator()
    {
        RuleFor(x => x.FulfilmentType).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.CouponCode).MaximumLength(40);
    }
}

public sealed class PlaceOrderRequestDtoValidator : AbstractValidator<PlaceOrderRequestDto>
{
    public PlaceOrderRequestDtoValidator()
    {
        RuleFor(x => x.FulfilmentType).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();

        RuleFor(x => x.AddressId)
            .NotEmpty()
            .When(x => x.FulfilmentType == FulfilmentType.Delivery)
            .WithMessage("Delivery address is required for delivery orders.");

        RuleFor(x => x.CouponCode).MaximumLength(40);
        RuleFor(x => x.CustomerNote).MaximumLength(500);
    }
}

public sealed class AcceptOrderDtoValidator : AbstractValidator<AcceptOrderDto>
{
    public AcceptOrderDtoValidator()
    {
        RuleFor(x => x.PrepMinutes).InclusiveBetween(5, 180);
    }
}

public sealed class RejectOrderDtoValidator : AbstractValidator<RejectOrderDto>
{
    public RejectOrderDtoValidator()
    {
        RuleFor(x => x.ReasonCode)
            .IsInEnum()
            .Must(code => Order.StoreAllowedCancelReasons.Contains(code))
            .WithMessage("Stores may only select store-applicable rejection reasons.");

        RuleFor(x => x.ReasonText)
            .NotEmpty()
            .When(x => x.ReasonCode == CancelReasonCode.Other)
            .WithMessage("Reason details are required when ReasonCode is Other.");

        RuleFor(x => x.ReasonText).MaximumLength(400);
    }
}

public sealed class CancelOrderDtoValidator : AbstractValidator<CancelOrderDto>
{
    public CancelOrderDtoValidator()
    {
        RuleFor(x => x.ReasonCode)
            .IsInEnum()
            .Must(code => Order.CustomerAllowedCancelReasons.Contains(code))
            .WithMessage("Customers may only select customer-applicable cancellation reasons.");

        RuleFor(x => x.ReasonText)
            .NotEmpty()
            .When(x => x.ReasonCode == CancelReasonCode.Other)
            .WithMessage("Reason details are required when ReasonCode is Other.");

        RuleFor(x => x.ReasonText).MaximumLength(400);
    }
}

public sealed class AdminChangeOrderStatusDtoValidator : AbstractValidator<AdminChangeOrderStatusDto>
{
    public AdminChangeOrderStatusDtoValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.ReasonCode)
            .IsInEnum()
            .When(x => x.ReasonCode.HasValue);
        RuleFor(x => x.ReasonText).NotEmpty().MaximumLength(500);
    }
}
