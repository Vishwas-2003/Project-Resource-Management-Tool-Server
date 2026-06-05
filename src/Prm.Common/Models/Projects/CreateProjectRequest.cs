using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Projects;

public class CreateProjectRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [Range(1, 3)]
    public int Status { get; set; }

    [Range(1, int.MaxValue)]
    public int ManagerUserId { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalStoryPoints { get; set; }
}
