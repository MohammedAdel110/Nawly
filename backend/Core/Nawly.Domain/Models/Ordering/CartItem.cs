using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Ordering;

public class CartItem : BaseGuidEntity
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }

    public Cart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<CartItemOption> Options { get; set; } = [];
}
