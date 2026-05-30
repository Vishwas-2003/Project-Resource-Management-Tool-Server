namespace Prm.Common.Models.Skills;

public class EmployeeSkillsResult
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<EmployeeSkillItem> Skills { get; set; } = [];
}
