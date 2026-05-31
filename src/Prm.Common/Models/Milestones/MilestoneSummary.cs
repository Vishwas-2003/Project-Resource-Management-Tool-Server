namespace Prm.Common.Models.Milestones;

public class MilestoneSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
