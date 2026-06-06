using Prm.Common.Constants;
using Prm.Common.Models.Timesheets;
using Prm.Data.Entities;

namespace Prm.Api.Services;

public partial class TimesheetService
{
    private sealed record SubmitContext(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        int MaxWeeklyHours,
        IReadOnlyDictionary<int, Allocation> AllocationByProject);

    public async Task<SubmitTimesheetResponse> SubmitTimesheet(
        int userId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeByUserIdOrThrow(userId, cancellationToken);
        var context = await PrepareSubmitContext(employee.Id, request, cancellationToken);
        var (entries, totalHours) = await BuildSubmitEntries(request.Entries, context, cancellationToken);
        ValidateSubmitTotalHours(totalHours, context.MaxWeeklyHours);

        return await SaveSubmittedTimesheet(employee.Id, context.WeekStart, totalHours, entries, cancellationToken);
    }

    private async Task<SubmitContext> PrepareSubmitContext(
        int employeeId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken)
    {
        var weekStart = TimesheetWeekHelper.GetWeekStart(request.WeekStart);
        ValidateWeekStartIsMonday(request.WeekStart, weekStart);
        await ValidateSubmitWeekAndRequest(employeeId, weekStart, request, cancellationToken);

        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        var maxWeeklyHours = await GetMaxWeeklyHours(cancellationToken);
        var allocations = await GetAllocationsOverlappingWeek(employeeId, weekStart, weekEnd, cancellationToken);

        return new SubmitContext(
            weekStart,
            weekEnd,
            maxWeeklyHours,
            allocations.ToDictionary(x => x.ProjectId));
    }

    private async Task ValidateSubmitWeekAndRequest(
        int employeeId,
        DateOnly weekStart,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (weekStart > TimesheetWeekHelper.GetWeekStart(today))
        {
            throw new ArgumentException(AppConstants.Timesheets.FutureWeekNotAllowed);
        }

        if (await _timesheetRepository.ExistsForEmployeeWeek(employeeId, weekStart, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Timesheets.AlreadySubmitted);
        }

        if (request.Entries.Count == 0)
        {
            throw new ArgumentException(AppConstants.Timesheets.NoEntries);
        }

        if (request.Entries.Select(x => x.ProjectId).Distinct().Count() != request.Entries.Count)
        {
            throw new ArgumentException(AppConstants.Timesheets.DuplicateProjectInEntries);
        }
    }

    private async Task<(List<TimesheetEntry> Entries, int TotalHours)> BuildSubmitEntries(
        IReadOnlyList<TimesheetEntryRequest> entryRequests,
        SubmitContext context,
        CancellationToken cancellationToken)
    {
        var entries = new List<TimesheetEntry>();
        var totalHours = 0;

        foreach (var entryRequest in entryRequests)
        {
            var entry = await BuildSubmitEntry(entryRequest, context, cancellationToken);
            if (entry is null)
            {
                continue;
            }

            entries.Add(entry);
            totalHours += entryRequest.HoursWorked;
        }

        return (entries, totalHours);
    }

    private async Task<TimesheetEntry?> BuildSubmitEntry(
        TimesheetEntryRequest entryRequest,
        SubmitContext context,
        CancellationToken cancellationToken)
    {
        if (!context.AllocationByProject.TryGetValue(entryRequest.ProjectId, out var allocation))
        {
            throw new ArgumentException(AppConstants.Timesheets.ProjectNotAllocated);
        }

        var expectedHours = TimesheetWeekHelper.ComputeExpectedHours(
            allocation.UtilizationPercent,
            context.MaxWeeklyHours);
        if (entryRequest.HoursWorked > expectedHours)
        {
            throw new ArgumentException(AppConstants.Timesheets.HoursExceedAllocation);
        }

        if (entryRequest.HoursWorked <= 0)
        {
            return null;
        }

        var (tagIds, otherTags) = await ResolveEntryTagSelection(entryRequest, cancellationToken);
        if (tagIds.Count == 0 && otherTags.Count == 0)
        {
            throw new ArgumentException(AppConstants.Timesheets.ActivityTagsRequired);
        }

        var resolvedTags = await ResolveActivityTags(tagIds, otherTags, cancellationToken);
        return new TimesheetEntry
        {
            ProjectId = entryRequest.ProjectId,
            HoursWorked = entryRequest.HoursWorked,
            ActivityTags = resolvedTags.Select(tag => new TimesheetActivityTag
            {
                ActivityTag = tag,
            }).ToList(),
        };
    }

    private static void ValidateSubmitTotalHours(int totalHours, int maxWeeklyHours)
    {
        if (totalHours > maxWeeklyHours)
        {
            throw new ArgumentException(AppConstants.Timesheets.TotalHoursExceedMax);
        }
    }

    private async Task<SubmitTimesheetResponse> SaveSubmittedTimesheet(
        int employeeId,
        DateOnly weekStart,
        int totalHours,
        List<TimesheetEntry> entries,
        CancellationToken cancellationToken)
    {
        var timesheet = new Timesheet
        {
            EmployeeId = employeeId,
            WeekStart = weekStart,
            TotalHours = totalHours,
            Status = TimesheetConstants.StatusSubmitted,
            Entries = entries,
        };

        await _timesheetRepository.Add(timesheet, cancellationToken);
        await _timesheetRepository.SaveChanges(cancellationToken);

        return new SubmitTimesheetResponse
        {
            TimesheetId = timesheet.Id,
            WeekStart = weekStart,
            TotalHours = totalHours,
            Status = TimesheetConstants.StatusSubmitted,
        };
    }
}
