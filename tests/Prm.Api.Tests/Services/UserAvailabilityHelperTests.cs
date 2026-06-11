using Prm.Api.Services;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;

namespace Prm.Api.Tests.Services;

public class UserAvailabilityHelperTests
{
    [Fact]
    public void EnsureWeekEligibleForUser_WhenWeekIsBeforeCreation_ThrowsArgumentException()
    {
        var user = ApiTestData.CreateResourceUser();
        user.CreatedAtUtc = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
        var weekStart = new DateOnly(2026, 6, 2);

        var exception = Assert.Throws<ArgumentException>(() =>
            UserAvailabilityHelper.EnsureWeekEligibleForUser(user, weekStart));

        Assert.Equal(AppConstants.Timesheets.WeekBeforeResourceCreated, exception.Message);
    }

    [Fact]
    public void EnsureAllocationDatesEligibleForUser_WhenFromDateBeforeCreation_ThrowsArgumentException()
    {
        var user = ApiTestData.CreateResourceUser();
        user.CreatedAtUtc = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<ArgumentException>(() =>
            UserAvailabilityHelper.EnsureAllocationDatesEligibleForUser(
                user,
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 6, 30)));

        Assert.Equal(AppConstants.Allocations.AllocationDatesBeforeResourceCreated, exception.Message);
    }

    [Fact]
    public void IsWeekEligibleForUser_WhenWeekContainsCreationDate_ReturnsTrue()
    {
        var user = ApiTestData.CreateResourceUser();
        user.CreatedAtUtc = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

        var result = UserAvailabilityHelper.IsWeekEligibleForUser(user, new DateOnly(2026, 6, 9));

        Assert.True(result);
    }
}
