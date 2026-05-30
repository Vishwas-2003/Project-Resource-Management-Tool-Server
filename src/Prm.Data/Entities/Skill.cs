namespace Prm.Data.Entities;

public class Skill : BaseEntity
{
    public int SkillId { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
}
