using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Address;
using Nawly.Shared.DTOs.Address;

namespace Nawly.Presentation.Controllers.Address;

[Authorize]
[Route("api/v1/addresses")]
public sealed class AddressesController(IAddressService addressService) : ApiBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetMyAddresses(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await addressService.GetMyAddressesAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await addressService.CreateAddressAsync(userId, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{addressId:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] UpdateAddressDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await addressService.UpdateAddressAsync(userId, addressId, request, ct);
        return HandleResult(result);
    }

    [HttpDelete("{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await addressService.DeleteAddressAsync(userId, addressId, ct);
        return HandleResult(result);
    }

    [HttpPost("{addressId:guid}/default")]
    public async Task<IActionResult> SetDefaultAddress(Guid addressId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await addressService.SetDefaultAddressAsync(userId, addressId, ct);
        return HandleResult(result);
    }
}
