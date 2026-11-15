using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;

namespace Nawly.Domain.Models.Engagement;

public class Favorite : BaseGuidEntity
{
    public Guid CustomerId { get; set; }
    public Guid StoreId { get; set; }

    public CustomerProfile Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
