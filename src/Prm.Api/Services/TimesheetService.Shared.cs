using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Timesheets;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public partial class TimesheetService
{
    private Task<IReadOnlyList<Allocation>> GetAllocationsOverlappingWeek(
        int userId,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken) =>
        _allocationRepository.GetOverlappingForUser(
            new UserAllocationPeriodQuery
            {
                UserId = userId,
                FromDate = weekStart,
                ToDate = weekEnd,
            },
            cancellationToken);

    private static IReadOnlyList<TimesheetEntryDetail> MapEntryDetails(IEnumerable<TimesheetEntry> entries) =>
        entries.Select(x => new TimesheetEntryDetail
        {
            ProjectName = x.Project.Name,
            HoursWorked = x.HoursWorked,
            ActivityTags = x.ActivityTags
                .Select(tag => tag.ActivityTag.Name)
                .OrderBy(name => name)
                .ToList(),
        }).ToList();

    private async Task<(IReadOnlyList<int> TagIds, IReadOnlyList<string> OtherTags)> ResolveEntryTagSelection(
        TimesheetEntryRequest entryRequest,
        CancellationToken cancellationToken)
    {
        var tagIds = entryRequest.ActivityTagIds?.ToList() ?? [];
        var otherTags = entryRequest.OtherActivityTags?.ToList() ?? [];

        if (entryRequest.ActivityTags is null || entryRequest.ActivityTags.Count == 0)
        {
            return (tagIds, otherTags);
        }

        var standardTags = await _timesheetRepository.GetAllActivityTags(cancellationToken);
        var standardTagByName = standardTags.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);
        var otherTagName = TimesheetConstants.StandardActivityTagNames[^1];

        foreach (var tagName in entryRequest.ActivityTags.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (standardTagByName.TryGetValue(tagName.Trim(), out var tagId)
                && !tagName.Trim().Equals(otherTagName, StringComparison.OrdinalIgnoreCase))
            {
                if (!tagIds.Contains(tagId))
                {
                    tagIds.Add(tagId);
                }
            }
            else if (!otherTags.Contains(tagName.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                otherTags.Add(tagName.Trim());
            }
        }

        return (tagIds, otherTags);
    }

    private async Task<IReadOnlyList<ActivityTag>> ResolveActivityTags(
        IReadOnlyCollection<int> activityTagIds,
        IReadOnlyCollection<string> otherActivityTags,
        CancellationToken cancellationToken)
    {
        var tags = new List<ActivityTag>();

        if (activityTagIds.Count > 0)
        {
            var existingTags = await _timesheetRepository.GetActivityTagsByIds(activityTagIds, cancellationToken);
            if (existingTags.Count != activityTagIds.Distinct().Count())
            {
                throw new ArgumentException(AppConstants.Timesheets.InvalidActivityTag);
            }

            tags.AddRange(existingTags);
        }

        foreach (var otherTag in otherActivityTags.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var tag = await _timesheetRepository.FindOrCreateActivityTagByName(otherTag, cancellationToken);
            if (tags.All(x => x.Name != tag.Name))
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    private async Task<User> GetResourceUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(userId, cancellationToken);
        if (user is null || user.RoleId != (int)RoleNameEnum.Employee)
        {
            throw new KeyNotFoundException(AppConstants.Timesheets.ResourceNotFound);
        }

        return user;
    }

    private async Task<int> GetMaxWeeklyHours(CancellationToken cancellationToken)
    {
        var config = await _systemConfigurationRepository.GetById(
            (int)ConfigurationOptionEnum.MaxWeeklyHours,
            cancellationToken);

        if (config is null || !int.TryParse(config.Value, out var hours) || hours <= 0)
        {
            return ManagerConstants.DefaultMaxWeeklyHours;
        }

        return hours;
    }

    private static void ValidateWeekStartIsMonday(DateOnly requestedWeekStart, DateOnly normalizedWeekStart)
    {
        if (requestedWeekStart != normalizedWeekStart)
        {
            throw new ArgumentException(AppConstants.Timesheets.InvalidWeekStart);
        }
    }
}
