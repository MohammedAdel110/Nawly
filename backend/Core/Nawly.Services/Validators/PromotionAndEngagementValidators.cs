using FluentValidation;
using Nawly.Shared.DTOs.Finance;
using Nawly.Shared.DTOs.Notification;
using Nawly.Shared.DTOs.Promotion;
using Nawly.Shared.DTOs.Review;
using Nawly.Shared.DTOs.Support;
using Nawly.Shared.EnumHelper.NotificationEnums;
using Nawly.Shared.EnumHelper.PromotionEnums;

namespace Nawly.Services.Validators;

public sealed class UpsertCouponDtoValidator : AbstractValidator<UpsertCouponDto>
{
    public UpsertCouponDtoValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.FundedBy).IsInEnum();

        RuleFor(x => x.Value)
            .GreaterThan(0m)
            .When(x => x.Type != CouponType.FreeDelivery);

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100m)
            .When(x => x.Type == CouponType.Percent);

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("Coupon end time must be after start time.");
    }
}

public sealed class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(x => x.StoreRating).InclusiveBetween(1, 5);
        RuleFor(x => x.CourierRating)
            .InclusiveBetween(1, 5)
            .When(x => x.CourierRating.HasValue);
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public sealed class CreateSupportTicketDtoValidator : AbstractValidator<CreateSupportTicketDto>
{
    public CreateSupportTicketDtoValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InitialMessage).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AttachmentRef).MaximumLength(300);
    }
}

public sealed class ReplySupportTicketDtoValidator : AbstractValidator<ReplySupportTicketDto>
{
    public ReplySupportTicketDtoValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AttachmentRef).MaximumLength(300);
        RuleFor(x => x.NewStatus).IsInEnum().When(x => x.NewStatus.HasValue);
    }
}

public sealed class UpsertBannerDtoValidator : AbstractValidator<UpsertBannerDto>
{
    public UpsertBannerDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ImageRef).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.TargetUrl).MaximumLength(500);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("Banner end time must be after start time.");
    }
}

public sealed class ModerateReviewDtoValidator : AbstractValidator<ModerateReviewDto>
{
    public ModerateReviewDtoValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class BroadcastNotificationDtoValidator : AbstractValidator<BroadcastNotificationDto>
{
    public BroadcastNotificationDtoValidator()
    {
        RuleFor(x => x.TargetRole).IsInEnum().When(x => x.TargetRole.HasValue);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Channels)
            .IsInEnum()
            .NotEqual(NotificationChannelFlags.None)
            .WithMessage("At least one valid notification channel must be selected.");
    }
}

public sealed class GenerateSettlementsRequestDtoValidator : AbstractValidator<GenerateSettlementsRequestDto>
{
    public GenerateSettlementsRequestDtoValidator()
    {
        RuleFor(x => x.PartyType).IsInEnum().When(x => x.PartyType.HasValue);
        RuleFor(x => x.PeriodEnd)
            .GreaterThan(x => x.PeriodStart)
            .WithMessage("Settlement period end must be after period start.");
    }
}
