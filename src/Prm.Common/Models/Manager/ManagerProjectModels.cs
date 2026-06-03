namespace Prm.Common.Models.Manager;

public class ManagerProjectListResult
{
    public IReadOnlyList<ManagerProjectSummary> Projects { get; set; } = [];
}

public class ManagerProjectSummary
{
    public int RowNumber { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
}

public class ManagerProjectDetailResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public IReadOnlyList<RiskFlagItem> RiskFlags { get; set; } = [];
    public IReadOnlyList<ManagerMilestoneRow> Milestones { get; set; } = [];
    public IReadOnlyList<ProjectResourceRow> AllocatedResources { get; set; } = [];
}

public class RiskFlagItem
{
    public string Outcome { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ManagerMilestoneRow
{
    public int RowNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

public class ProjectResourceRow
{
    public string Name { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}
