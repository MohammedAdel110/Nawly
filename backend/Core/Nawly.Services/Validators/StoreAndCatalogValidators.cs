using FluentValidation;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.DTOs.Catalog;
using Nawly.Shared.DTOs.Store;

namespace Nawly.Services.Validators;

public sealed class ApplyStoreDtoValidator : AbstractValidator<ApplyStoreDto>
{
    public ApplyStoreDtoValidator()
    {
        RuleFor(x => x.OwnerFullName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.OwnerPhone)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Owner phone must be a valid Egyptian mobile number.");

        RuleFor(x => x.OwnerPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.StoreNameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.AreaId).NotEmpty();
        RuleFor(x => x.AddressText).NotEmpty().MaximumLength(400);

        RuleFor(x => x.StorePhone)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Store phone must be a valid Egyptian mobile number.");

        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.DefaultPrepMinutes).InclusiveBetween(5, 180);
        RuleFor(x => x.DeliveryMode).IsInEnum();
        RuleFor(x => x.DeliveryZoneIds).NotEmpty();
    }
}

public sealed class UpsertMenuCategoryDtoValidator : AbstractValidator<UpsertMenuCategoryDto>
{
    public UpsertMenuCategoryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpsertProductDtoValidator : AbstractValidator<UpsertProductDto>
{
    public UpsertProductDtoValidator()
    {
        RuleFor(x => x.MenuCategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(180);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThan(0m);
        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(x => x.Price)
            .When(x => x.CompareAtPrice.HasValue);
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StockQuantity.HasValue);
        RuleFor(x => x.PrepMinutes)
            .InclusiveBetween(1, 180)
            .When(x => x.PrepMinutes.HasValue);
    }
}

public sealed class UpdateStoreSettingsDtoValidator : AbstractValidator<UpdateStoreSettingsDto>
{
    public UpdateStoreSettingsDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Store phone must be a valid Egyptian mobile number.");

        RuleFor(x => x.AddressText).NotEmpty().MaximumLength(400);
        RuleFor(x => x.Latitude).InclusiveBetween(-90d, 90d).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180d, 180d).When(x => x.Longitude.HasValue);
        RuleFor(x => x.DeliveryMode).IsInEnum();
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.DefaultPrepMinutes).InclusiveBetween(5, 180);
        RuleFor(x => x.BusyExtraMinutes).InclusiveBetween(0, 120);
    }
}

public sealed class AddStoreStaffDtoValidator : AbstractValidator<AddStoreStaffDto>
{
    public AddStoreStaffDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Staff phone number must be a valid Egyptian mobile number.");

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.StoreRole).IsInEnum();
    }
}
