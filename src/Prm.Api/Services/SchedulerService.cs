using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Models.Employees;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public class SchedulerService(
    IAllocationRepository _allocationRepository,
    IEmployeeRepository _employeeRepository,
    IProjectRepository _projectRepository,
    IProjectRiskFlagRepository _projectRiskFlagRepository,
    IProjectHealthService _projectHealthService,
    ILogger<SchedulerService> _logger) : ISchedulerService
{
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduler job started.");

        await UpdateEmployeesStatus(cancellationToken);
        await ComputeProjectHealth(cancellationToken);

        _logger.LogInformation("Scheduler job completed.");
    }

    private async Task UpdateEmployeesStatus(CancellationToken cancellationToken)
    {
        var employees = await _employeeRepository.GetEmployees(
            new EmployeeFilter { IncludeInactive = false },
            cancellationToken);

        foreach (var employee in employees)
        {
            await ApplyStatus(employee, cancellationToken);
            _employeeRepository.Update(employee);
        }

        await _employeeRepository.SaveChanges(cancellationToken);
        _logger.LogInformation("Updated bench status for {EmployeeCount} employees.", employees.Count);
    }

    private async Task ApplyStatus(
        Employee employee,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await _allocationRepository.SumUtilizationForEmployeeInPeriod(
            new EmployeeAllocationPeriodQuery
            {
                EmployeeId = employee.Id,
                FromDate = today,
                ToDate = today,
            },
            cancellationToken);

        employee.Status = utilization > 0
            ? EmployeeConstants.StatusAllocated
            : EmployeeConstants.StatusBench;
    }

    private async Task ComputeProjectHealth(CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.GetAllWithManager(cancellationToken);
        var updatedCount = 0;

        foreach (var project in projects)
        {
            var projectDetails = await _projectRepository.GetByIdWithDetails(project.Id, cancellationToken);
            if (projectDetails is null)
            {
                continue;
            }

            var evaluation = await _projectHealthService.Evaluate(projectDetails, cancellationToken);
            projectDetails.HealthStatus = evaluation.HealthStatus;
            _projectRepository.Update(projectDetails);

            await _projectRiskFlagRepository.ReplaceForProject(
                projectDetails.Id,
                ToEntities(evaluation.RiskFlags),
                cancellationToken);

            updatedCount++;
        }

        await _projectRepository.SaveChanges(cancellationToken);
        _logger.LogInformation(
            "Updated health and risk flags for {UpdatedCount} of {ProjectCount} projects.",
            updatedCount,
            projects.Count);
    }

    private static IReadOnlyList<ProjectRiskFlag> ToEntities(IReadOnlyList<RiskFlagItem> riskFlags)
    {
        return riskFlags
            .Select((flag, index) => new ProjectRiskFlag
            {
                SortOrder = index + 1,
                Outcome = flag.Outcome,
                Message = flag.Message,
            })
            .ToList();
    }
}
