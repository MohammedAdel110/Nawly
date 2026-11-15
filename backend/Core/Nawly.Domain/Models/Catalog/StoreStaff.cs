using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.EnumHelper.StoreEnums;

namespace Nawly.Domain.Models.Catalog;

public class StoreStaff : BaseGuidEntity
{
    public Guid StoreId { get; set; }
    public Guid UserId { get; set; }
    public StoreRole StoreRole { get; set; } = StoreRole.OrderHandler;
    public bool IsActive { get; set; } = true;

    public Store Store { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
