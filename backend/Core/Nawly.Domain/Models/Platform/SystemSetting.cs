using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Platform;

public class SystemSetting : BaseGuidEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? Description { get; set; }
}
