using Nawly.Abstraction.IServices.Address;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Geography;
using Nawly.Domain.ValueObjects;
using Nawly.Services.Mapping.AddressMapping;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Services.Specifications.StoreSpec;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Address;
using AddressEntity = Nawly.Domain.Models.Geography.Address;

namespace Nawly.Services.ServicesImplementation.Address;

public sealed class AddressService(
    IUnitOfWork unitOfWork,
    IClock clock) : IAddressService
{
    public async Task<Result<IReadOnlyList<AddressDto>>> GetMyAddressesAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Success<IReadOnlyList<AddressDto>>([]);

        var addresses = await unitOfWork
            .GetRepository<AddressEntity, Guid>()
            .GetAllWithSpecAsync(new AddressesByCustomerSpecification(customer.Id), ct);

        return Result.Success<IReadOnlyList<AddressDto>>(addresses.Select(a => a.ToDto()).ToList());
    }

    public async Task<Result<AddressDto>> CreateAddressAsync(Guid userId, CreateAddressDto request, CancellationToken ct = default)
    {
        if (!PhoneNumber.TryParseEgyptianMobile(request.ContactPhone, out var normalizedPhone) || normalizedPhone is null)
            return Result.Failure<AddressDto>(Error.Validation("address.invalid_phone", "Contact phone must be a valid Egyptian mobile number."));

        var area = await unitOfWork
            .GetRepository<Area, Guid>()
            .GetByIdWithSpecAsync(new AreaWithZoneByIdSpecification(request.AreaId), ct);

        if (area is null || !area.IsActive || !area.Zone.IsActive)
            return Result.Failure<AddressDto>(Error.NotFound("address.area_not_found", "Selected delivery area is unavailable."));

        var customer = await ServiceHelpers.ResolveOrCreateCustomerProfileAsync(unitOfWork, userId, ct);
        var addressRepo = unitOfWork.GetRepository<AddressEntity, Guid>();
        var existing = await addressRepo.GetAllWithSpecAsync(new AddressesByCustomerSpecification(customer.Id), ct);

        if (existing.Count >= AddressEntity.MaxAddressesPerCustomer)
            return Result.Failure<AddressDto>(Error.Unprocessable("address.limit_exceeded", $"You can save up to {AddressEntity.MaxAddressesPerCustomer} addresses."));

        var shouldBeDefault = request.IsDefault || existing.Count == 0;
        if (shouldBeDefault)
            ClearDefaults(existing, addressRepo);

        var address = new AddressEntity
        {
            CustomerId = customer.Id,
            Label = request.Label.Trim(),
            AreaId = area.Id,
            Area = area,
            Description = request.Description.Trim(),
            Building = request.Building?.Trim(),
            Floor = request.Floor?.Trim(),
            Apartment = request.Apartment?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ContactPhone = normalizedPhone.Value,
            Notes = request.Notes?.Trim(),
            IsDefault = shouldBeDefault,
            CreatedAt = clock.UtcNow
        };

        await addressRepo.AddAsync(address, ct);
        if (shouldBeDefault)
            customer.DefaultAddressId = address.Id;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(address.ToDto());
    }

    public async Task<Result<AddressDto>> UpdateAddressAsync(
        Guid userId,
        Guid addressId,
        UpdateAddressDto request,
        CancellationToken ct = default)
    {
        if (!PhoneNumber.TryParseEgyptianMobile(request.ContactPhone, out var normalizedPhone) || normalizedPhone is null)
            return Result.Failure<AddressDto>(Error.Validation("address.invalid_phone", "Contact phone must be a valid Egyptian mobile number."));

        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Failure<AddressDto>(Error.NotFound("address.not_found", "Address not found."));

        var addressRepo = unitOfWork.GetRepository<AddressEntity, Guid>();
        var address = await addressRepo.GetByIdWithSpecAsync(new AddressByIdSpecification(addressId, customer.Id), ct);
        if (address is null)
            return Result.Failure<AddressDto>(Error.NotFound("address.not_found", "Address not found."));

        var area = await unitOfWork
            .GetRepository<Area, Guid>()
            .GetByIdWithSpecAsync(new AreaWithZoneByIdSpecification(request.AreaId), ct);

        if (area is null || !area.IsActive || !area.Zone.IsActive)
            return Result.Failure<AddressDto>(Error.NotFound("address.area_not_found", "Selected delivery area is unavailable."));

        if (request.IsDefault && !address.IsDefault)
        {
            var allAddresses = await addressRepo.GetAllWithSpecAsync(new AddressesByCustomerSpecification(customer.Id), ct);
            ClearDefaults(allAddresses, addressRepo);
            customer.DefaultAddressId = address.Id;
        }

        address.Label = request.Label.Trim();
        address.AreaId = area.Id;
        address.Area = area;
        address.Description = request.Description.Trim();
        address.Building = request.Building?.Trim();
        address.Floor = request.Floor?.Trim();
        address.Apartment = request.Apartment?.Trim();
        address.Latitude = request.Latitude;
        address.Longitude = request.Longitude;
        address.ContactPhone = normalizedPhone.Value;
        address.Notes = request.Notes?.Trim();
        address.IsDefault = request.IsDefault;
        address.UpdatedAt = clock.UtcNow;

        addressRepo.Update(address);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(address.ToDto());
    }

    public async Task<Result> DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Failure(Error.NotFound("address.not_found", "Address not found."));

        var addressRepo = unitOfWork.GetRepository<AddressEntity, Guid>();
        var address = await addressRepo.GetByIdWithSpecAsync(new AddressByIdSpecification(addressId, customer.Id), ct);
        if (address is null)
            return Result.Failure(Error.NotFound("address.not_found", "Address not found."));

        address.IsDeleted = true;
        address.DeletedAt = clock.UtcNow;
        addressRepo.Update(address);

        if (customer.DefaultAddressId == address.Id)
            customer.DefaultAddressId = null;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<AddressDto>> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Failure<AddressDto>(Error.NotFound("address.not_found", "Address not found."));

        var addressRepo = unitOfWork.GetRepository<AddressEntity, Guid>();
        var allAddresses = await addressRepo.GetAllWithSpecAsync(new AddressesByCustomerSpecification(customer.Id), ct);
        var target = allAddresses.FirstOrDefault(a => a.Id == addressId);
        if (target is null)
            return Result.Failure<AddressDto>(Error.NotFound("address.not_found", "Address not found."));

        ClearDefaults(allAddresses, addressRepo);
        target.IsDefault = true;
        target.UpdatedAt = clock.UtcNow;
        customer.DefaultAddressId = target.Id;

        addressRepo.Update(target);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(target.ToDto());
    }

    private static void ClearDefaults(IEnumerable<AddressEntity> addresses, Domain.Contracts.GenericReposPattern.IGenericRepo<AddressEntity, Guid> repo)
    {
        foreach (var addr in addresses.Where(a => a.IsDefault))
        {
            addr.IsDefault = false;
            repo.Update(addr);
        }
    }
}
