using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Ordering;

public class CartItemOption : BaseGuidEntity
{
    public Guid CartItemId { get; set; }
    public Guid ProductOptionId { get; set; }

    public CartItem CartItem { get; set; } = null!;
    public ProductOption ProductOption { get; set; } = null!;
}
