using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Cart;

namespace Nawly.Abstraction.IServices.Cart;

public interface ICartService
{
    Task<Result<CartDto?>> GetActiveCartAsync(Guid userId, CancellationToken ct = default);
    Task<Result<CartDto>> UpsertItemAsync(Guid userId, UpsertCartItemDto request, CancellationToken ct = default);
    Task<Result<CartDto>> RemoveItemAsync(Guid userId, Guid cartItemId, CancellationToken ct = default);
    Task<Result> ClearCartAsync(Guid userId, CancellationToken ct = default);
    Task<Result<CartQuoteResponseDto>> QuoteCartAsync(Guid userId, CartQuoteRequestDto request, CancellationToken ct = default);
}
