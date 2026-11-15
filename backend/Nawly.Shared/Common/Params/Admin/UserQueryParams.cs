using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Shared.Common.Params.Admin;

public class UserQueryParams : BaseQueryParam
{
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
}
