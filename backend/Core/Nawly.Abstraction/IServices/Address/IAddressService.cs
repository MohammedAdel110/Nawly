using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Address;

namespace Nawly.Abstraction.IServices.Address;

public interface IAddressService
{
    Task<Result<IReadOnlyList<AddressDto>>> GetMyAddressesAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AddressDto>> CreateAddressAsync(Guid userId, CreateAddressDto request, CancellationToken ct = default);
    Task<Result<AddressDto>> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressDto request, CancellationToken ct = default);
    Task<Result> DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default);
    Task<Result<AddressDto>> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default);
}
