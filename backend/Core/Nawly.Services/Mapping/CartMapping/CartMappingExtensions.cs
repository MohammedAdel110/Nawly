using Nawly.Domain.Models.Ordering;
using Nawly.Shared.DTOs.Cart;

namespace Nawly.Services.Mapping.CartMapping;

public static class CartMappingExtensions
{
    public static CartItemOptionDto ToDto(this CartItemOption option) => new(
        option.ProductOptionId,
        option.ProductOption?.Group?.Name ?? string.Empty,
        option.ProductOption?.Group?.NameAr ?? string.Empty,
        option.ProductOption?.Name ?? string.Empty,
        option.ProductOption?.NameAr ?? string.Empty,
        option.ProductOption?.PriceDelta ?? 0m);

    public static CartItemDto ToDto(this CartItem item)
    {
        var options = item.Options
            .Where(o => !o.IsDeleted)
            .Select(o => o.ToDto())
            .ToList();

        var basePrice = item.Product?.Price ?? 0m;
        var unitTotal = basePrice + options.Sum(o => o.PriceDelta);
        var lineTotal = decimal.Round(unitTotal * item.Quantity, 2, MidpointRounding.AwayFromZero);

        return new CartItemDto(
            item.Id,
            item.ProductId,
            item.Product?.Name ?? string.Empty,
            item.Product?.NameAr ?? string.Empty,
            item.Product?.ImageRef,
            basePrice,
            item.Quantity,
            lineTotal,
            item.Note,
            item.Product?.IsOrderable ?? false,
            options);
    }

    public static CartDto ToDto(this Cart cart)
    {
        var items = cart.Items
            .Where(i => !i.IsDeleted)
            .Select(i => i.ToDto())
            .ToList();

        var subtotal = decimal.Round(items.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero);

        return new CartDto(
            cart.Id,
            cart.StoreId,
            cart.Store?.Name ?? string.Empty,
            cart.Store?.NameAr ?? string.Empty,
            subtotal,
            items);
    }
}
