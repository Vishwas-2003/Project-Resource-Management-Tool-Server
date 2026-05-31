using Prm.Common.Enums;

namespace Prm.Data.Entities;

public class SystemConfiguration : BaseEntity
{
    public int Id { get; set; }
    public required string ConfigurationType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
