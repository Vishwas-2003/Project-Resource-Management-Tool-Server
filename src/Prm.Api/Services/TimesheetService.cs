using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Models.Timesheets;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public partial class TimesheetService(
    ITimesheetRepository _timesheetRepository,
    IUserRepository _userRepository,
    IAllocationRepository _allocationRepository,
    ISystemConfigurationRepository _systemConfigurationRepository) : ITimesheetService
{
    public async Task<ActivityTagsResponse> GetActivityTags(
        CancellationToken cancellationToken = default)
    {
        var tags = await _timesheetRepository.GetAllActivityTags(cancellationToken);
        var otherTagName = TimesheetConstants.StandardActivityTagNames[^1];
        var options = tags
            .Select((tag, index) => new ActivityTagOption
            {
                RowNumber = index + 1,
                Name = tag.Name,
                IsOther = tag.Name == otherTagName,
            })
            .ToList();

        return new ActivityTagsResponse { Tags = options };
    }

    public async Task<MissingTimesheetReminder> GetMissingReminder(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourceUserOrThrow(userId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastCompletedWeekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(today);
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(lastCompletedWeekStart);

        if (!UserAvailabilityHelper.IsWeekEligibleForUser(user, lastCompletedWeekStart))
        {
            return new MissingTimesheetReminder { HasMissing = false };
        }

        var allocations = await GetAllocationsOverlappingWeek(
            user.Id,
            lastCompletedWeekStart,
            weekEnd,
            cancellationToken);
        if (allocations.Count == 0)
        {
            return new MissingTimesheetReminder { HasMissing = false };
        }

        var submitted = await _timesheetRepository.IsSubmittedForUserWeek(
            user.Id,
            lastCompletedWeekStart,
            cancellationToken);

        return new MissingTimesheetReminder
        {
            HasMissing = !submitted,
            WeekStart = submitted ? null : lastCompletedWeekStart,
        };
    }

    public async Task<WeekAllocationsResponse> GetWeekAllocations(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourceUserOrThrow(userId, cancellationToken);
        var normalizedWeekStart = TimesheetWeekHelper.GetWeekStart(weekStart);
        UserAvailabilityHelper.EnsureWeekEligibleForUser(user, normalizedWeekStart);
        await EnsureTimesheetAccessAllowed(user.Id, normalizedWeekStart, cancellationToken);
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(normalizedWeekStart);
        var maxWeeklyHours = await GetMaxWeeklyHours(cancellationToken);

        var allocations = await GetAllocationsOverlappingWeek(
            user.Id,
            normalizedWeekStart,
            weekEnd,
            cancellationToken);

        return new WeekAllocationsResponse
        {
            ResourceName = user.FullName,
            WeekStart = normalizedWeekStart,
            MaxWeeklyHours = maxWeeklyHours,
            Allocations = allocations.Select(x => new WeekAllocationRow
            {
                ProjectId = x.ProjectId,
                ProjectName = x.Project.Name,
                UtilizationPercent = x.UtilizationPercent,
                MaxHours = TimesheetWeekHelper.ComputeExpectedHours(x.UtilizationPercent, maxWeeklyHours),
            }).ToList(),
        };
    }

    public async Task<ResourceAllocationsResponse> GetMyAllocations(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourceUserOrThrow(userId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allocations = await _allocationRepository.GetActiveByUserId(user.Id, today, cancellationToken);

        return new ResourceAllocationsResponse
        {
            Allocations = allocations.Select(x => new ResourceAllocationItem
            {
                ProjectName = x.Project.Name,
                UtilizationPercent = x.UtilizationPercent,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                Status = x.ToDate >= today
                    ? TimesheetConstants.AllocationStatusActive
                    : TimesheetConstants.AllocationStatusEnded,
            }).ToList(),
            TotalUtilizationPercent = allocations.Sum(x => x.UtilizationPercent),
        };
    }
}
