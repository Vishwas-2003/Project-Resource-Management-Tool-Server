namespace Prm.Api.Models.Skills;

public class EmployeeSkillsResponse
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<EmployeeSkillItemResponse> Skills { get; set; } = [];
}
