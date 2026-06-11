using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Skills;

public class AddResourceSkillRequest
{
    [Required]
    [MaxLength(100)]
    public string SkillName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Proficiency { get; set; } = string.Empty;
}
