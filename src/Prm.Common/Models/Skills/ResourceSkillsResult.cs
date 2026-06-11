namespace Prm.Common.Models.Skills;

public class ResourceSkillsResult
{
    public int ResourceUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<ResourceSkillItem> Skills { get; set; } = [];
}
