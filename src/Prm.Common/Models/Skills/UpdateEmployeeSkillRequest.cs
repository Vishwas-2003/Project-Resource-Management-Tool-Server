using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Skills;

public class UpdateEmployeeSkillRequest
{
    [Required]
    [MaxLength(50)]
    public string Proficiency { get; set; } = string.Empty;
}
