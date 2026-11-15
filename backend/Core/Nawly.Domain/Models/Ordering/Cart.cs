using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.Common.Results;

namespace Nawly.Domain.Models.Ordering;

public class Cart : AggregateRoot
{
    public const int MaxLineItems = 50;
    public const int MaxQuantityPerItem = 99;

    public Guid CustomerId { get; set; }
    public Guid StoreId { get; set; }
    public uint RowVersion { get; set; }

    public CustomerProfile Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = [];

    public Result UpsertItem(
        Guid productId,
        int quantity,
        string? note,
        IEnumerable<Guid> selectedOptionIds,
        DateTimeOffset utcNow,
        bool replaceCartIfDifferentStore = false,
        Guid? productStoreId = null)
    {
        if (quantity is < 1 or > MaxQuantityPerItem)
            return Result.Failure(Error.Validation("cart.invalid_quantity", $"Quantity must be between 1 and {MaxQuantityPerItem}."));

        if (productStoreId.HasValue && productStoreId.Value != StoreId)
        {
            if (!replaceCartIfDifferentStore && Items.Count > 0)
                return Result.Failure(Error.Conflict("cart.different_store", "Cart contains items from another store."));

            Clear(productStoreId.Value, utcNow);
        }

        var optionSet = selectedOptionIds.Distinct().OrderBy(x => x).ToList();
        var existing = Items.FirstOrDefault(i =>
            i.ProductId == productId &&
            i.Note == note &&
            i.Options.Select(o => o.ProductOptionId).OrderBy(x => x).SequenceEqual(optionSet));

        if (existing is not null)
        {
            var newQty = Math.Min(MaxQuantityPerItem, existing.Quantity + quantity);
            existing.Quantity = newQty;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        if (Items.Count >= MaxLineItems)
            return Result.Failure(Error.Unprocessable("cart.max_items_exceeded", $"A cart may contain at most {MaxLineItems} line items."));

        var item = new CartItem
        {
            CartId = Id,
            ProductId = productId,
            Quantity = quantity,
            Note = note,
            Options = optionSet.Select(optId => new CartItemOption { ProductOptionId = optId }).ToList()
        };

        Items.Add(item);
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result RemoveItem(Guid cartItemId, DateTimeOffset utcNow)
    {
        var item = Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null)
            return Result.Failure(Error.NotFound("cart.item_not_found", "Cart item not found."));

        Items.Remove(item);
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public void Clear(Guid newStoreId, DateTimeOffset utcNow)
    {
        Items.Clear();
        StoreId = newStoreId;
        UpdatedAt = utcNow;
    }
}
