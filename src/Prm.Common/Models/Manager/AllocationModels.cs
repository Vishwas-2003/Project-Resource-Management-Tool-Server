namespace Prm.Common.Models.Manager;

public class CreateAllocationRequest
{
    public int ProjectId { get; set; }
    public int EmployeeId { get; set; }
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public class AllocationCreatedResponse
{
    public int AllocationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public class ProjectAllocationsResponse
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public IReadOnlyList<ProjectAllocationRow> Allocations { get; set; } = [];
}

public class ProjectAllocationRow
{
    public int AllocationId { get; set; }
    public int RowNumber { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public class AllocationEndedResponse
{
    public int AllocationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
}
