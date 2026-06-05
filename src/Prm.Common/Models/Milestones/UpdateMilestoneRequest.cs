using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Milestones;

public class UpdateMilestoneRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateOnly DueDate { get; set; }

    [Range(0, int.MaxValue)]
    public int StoryPoints { get; set; }

    [Range(1, 3)]
    public int Status { get; set; }
}
