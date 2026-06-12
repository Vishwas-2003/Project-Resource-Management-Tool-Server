using Moq;
using Prm.Api.Services;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Timesheets;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class TimesheetServiceTests
{
    private readonly Mock<ITimesheetRepository> _timesheetRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAllocationRepository> _allocationRepository = new();
    private readonly Mock<ISystemConfigurationRepository> _systemConfigurationRepository = new();

    [Fact]
    public async Task GetActivityTags_ReturnsTagsWithIsOtherOnLastStandardTag()
    {
        var tags = ApiTestData.CreateStandardActivityTags();
        _timesheetRepository
            .Setup(x => x.GetAllActivityTags(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var sut = CreateSut();
        var result = await sut.GetActivityTags();

        var lastTag = result.Tags[^1];
        Assert.Equal(TimesheetConstants.StandardActivityTagNames[^1], lastTag.Name);
        Assert.True(lastTag.IsOther);
        Assert.All(result.Tags.Take(result.Tags.Count - 1), tag => Assert.False(tag.IsOther));
    }

    [Fact]
    public async Task GetMissingReminder_WhenNoAllocations_ReturnsHasMissingFalse()
    {
        var employee = ApiTestData.CreateResourceUser();
        SetupEmployeeByUserId(employee);
        SetupOverlappingAllocations(employee.Id, Array.Empty<Allocation>());

        var sut = CreateSut();
        var result = await sut.GetMissingReminder(employee.Id);

        Assert.False(result.HasMissing);
        Assert.Null(result.WeekStart);
    }

    [Fact]
    public async Task GetWeekAllocations_WhenResourceNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetById(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();
        var weekStart = TimesheetWeekHelper.GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetWeekAllocations(99, weekStart));

        Assert.Equal(AppConstants.Timesheets.ResourceNotFound, exception.Message);
    }

    [Fact]
    public async Task GetMyAllocations_ReturnsActiveAllocations()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employee = ApiTestData.CreateResourceUser();
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(
                id: 1,
                utilizationPercent: 60,
                fromDate: today.AddDays(-7),
                toDate: today.AddDays(30)),
            ApiTestData.CreateAllocation(
                id: 2,
                projectId: 2,
                projectName: "Beta",
                utilizationPercent: 40,
                fromDate: today.AddDays(-7),
                toDate: today.AddDays(-1)),
        };

        SetupEmployeeByUserId(employee);
        _allocationRepository
            .Setup(x => x.GetActiveByUserId(employee.Id, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        var sut = CreateSut();
        var result = await sut.GetMyAllocations(employee.Id);

        Assert.Equal(2, result.Allocations.Count);
        Assert.Equal(100, result.TotalUtilizationPercent);
        Assert.Equal(TimesheetConstants.AllocationStatusActive, result.Allocations[0].Status);
        Assert.Equal(TimesheetConstants.AllocationStatusEnded, result.Allocations[1].Status);
        Assert.Equal("Alpha", result.Allocations[0].ProjectName);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenAlreadySubmitted_ThrowsInvalidOperationException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: true);

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [1] },
            ],
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.AlreadySubmitted, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenFutureWeek_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var futureWeekStart = TimesheetWeekHelper.GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(7);

        SetupEmployeeByUserId(employee);

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = futureWeekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [1] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.FutureWeekNotAllowed, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenWeekBeforeResourceCreated_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        employee.CreatedAtUtc = DateTime.UtcNow;
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        SetupEmployeeByUserId(employee);

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [1] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.WeekBeforeResourceCreated, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenNoEntries_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        SetupEmployeeByUserId(employee);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries = [],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.NoEntries, exception.Message);
    }

    [Fact]
    public async Task GetMyTimesheets_ReturnsSubmittedSummaries()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var timesheets = new List<Timesheet>
        {
            new()
            {
                Id = 1,
                UserId = employee.Id,
                WeekStart = weekStart,
                TotalHours = 32,
                Status = TimesheetConstants.StatusSubmitted,
                Access = TimesheetConstants.AccessAllowed,
            },
        };

        SetupEmployeeByUserId(employee);
        _timesheetRepository
            .Setup(x => x.GetByUserId(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheets);
        SetupOverlappingAllocations(employee.Id, Array.Empty<Allocation>());

        var sut = CreateSut();
        var result = await sut.GetMyTimesheets(employee.Id);

        Assert.Single(result.Timesheets);
        Assert.Equal(weekStart, result.Timesheets[0].WeekStart);
        Assert.Equal(32, result.Timesheets[0].TotalHours);
        Assert.Equal(TimesheetConstants.StatusSubmitted, result.Timesheets[0].Status);
    }

    [Fact]
    public async Task GetTeamTimesheets_ReturnsRowsForManager()
    {
        const int managerUserId = 10;
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var submittedRows = new List<TeamTimesheetEntryRow>
        {
            new()
            {
                UserId = 1,
                UserName = "Jane Doe",
                ProjectName = "Alpha",
                Hours = 32,
                Status = TimesheetConstants.StatusSubmitted,
            },
        };

        _timesheetRepository
            .Setup(x => x.GetEntriesForTeamByManagerAndWeek(
                managerUserId,
                weekStart,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(submittedRows);
        _userRepository
            .Setup(x => x.GetResourceUsersByManagerUserId(managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        var sut = CreateSut();
        var result = await sut.GetTeamTimesheets(managerUserId, weekStart);

        Assert.Equal(weekStart, result.WeekStart);
        Assert.Single(result.Rows);
        Assert.Equal("Jane Doe", result.Rows[0].ResourceName);
        Assert.Equal("Alpha", result.Rows[0].ProjectName);
        Assert.Equal(32, result.Rows[0].HoursWorked);
        Assert.Equal(TimesheetConstants.StatusSubmitted, result.Rows[0].Status);
    }

    [Fact]
    public async Task GetMissingReminder_WhenAllocationExistsWithoutTimesheet_ReturnsHasMissingTrue()
    {
        var employee = ApiTestData.CreateResourceUser();
        var lastCompletedWeekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            fromDate: lastCompletedWeekStart,
            toDate: lastCompletedWeekStart.AddDays(6));

        SetupEmployeeByUserId(employee);
        SetupOverlappingAllocations(employee.Id, [allocation]);
        _timesheetRepository
            .Setup(x => x.IsSubmittedForUserWeek(employee.Id, lastCompletedWeekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.GetMissingReminder(employee.Id);

        Assert.True(result.HasMissing);
        Assert.Equal(lastCompletedWeekStart, result.WeekStart);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenValid_ReturnsSubmittedResponse()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            projectId: 1,
            utilizationPercent: 50,
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));
        var tags = ApiTestData.CreateStandardActivityTags();

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);
        SetupOverlappingAllocations(employee.Id, [allocation]);
        _timesheetRepository
            .Setup(x => x.GetActivityTagsByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                tags.Where(tag => ids.Contains(tag.Id)).ToList());
        _timesheetRepository
            .Setup(x => x.Add(It.IsAny<Timesheet>(), It.IsAny<CancellationToken>()))
            .Callback<Timesheet, CancellationToken>((timesheet, _) => timesheet.Id = 100)
            .Returns(Task.CompletedTask);
        _timesheetRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.SubmitTimesheet(
            employee.Id,
            new SubmitTimesheetRequest
            {
                WeekStart = weekStart,
                Entries =
                [
                    new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [tags[0].Id] },
                ],
            });

        Assert.Equal(100, result.TimesheetId);
        Assert.Equal(weekStart, result.WeekStart);
        Assert.Equal(8, result.TotalHours);
        Assert.Equal(TimesheetConstants.StatusSubmitted, result.Status);
    }

    [Fact]
    public async Task GetMyTimesheetDetail_WhenSubmitted_ReturnsDetail()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var project = ApiTestData.CreateProject();
        var timesheet = new Timesheet
        {
            Id = 1,
            UserId = employee.Id,
            WeekStart = weekStart,
            TotalHours = 16,
            Status = TimesheetConstants.StatusSubmitted,
            Access = TimesheetConstants.AccessAllowed,
            Entries =
            [
                new TimesheetEntry
                {
                    ProjectId = project.Id,
                    HoursWorked = 16,
                    Project = project,
                    ActivityTags =
                    [
                        new TimesheetActivityTag
                        {
                            ActivityTag = ApiTestData.CreateActivityTag(),
                        },
                    ],
                },
            ],
        };

        SetupEmployeeByUserId(employee);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheet);

        var sut = CreateSut();
        var result = await sut.GetMyTimesheetDetail(employee.Id, weekStart);

        Assert.Equal(TimesheetConstants.StatusSubmitted, result.Status);
        Assert.Equal(16, result.TotalHours);
        Assert.Single(result.Entries);
    }

    [Fact]
    public async Task GetMyTimesheetDetail_WhenMissedWeekWithAllocation_ReturnsMissedStatus()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));

        SetupEmployeeByUserId(employee);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Timesheet?)null);
        SetupOverlappingAllocations(employee.Id, [allocation]);

        var sut = CreateSut();
        var result = await sut.GetMyTimesheetDetail(employee.Id, weekStart);

        Assert.Equal(TimesheetConstants.StatusMissed, result.Status);
        Assert.Equal(0, result.TotalHours);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task GetResourceTimesheetDetail_WhenSubmitted_ReturnsDetail()
    {
        const int managerUserId = 10;
        var employee = ApiTestData.CreateResourceUser(managerUserId: managerUserId);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var project = ApiTestData.CreateProject();
        var timesheet = new Timesheet
        {
            Id = 1,
            UserId = employee.Id,
            WeekStart = weekStart,
            TotalHours = 20,
            Status = TimesheetConstants.StatusSubmitted,
            Access = TimesheetConstants.AccessAllowed,
            Entries =
            [
                new TimesheetEntry
                {
                    ProjectId = project.Id,
                    HoursWorked = 20,
                    Project = project,
                    ActivityTags = [],
                },
            ],
        };

        _userRepository
            .Setup(x => x.GetResourceUsersByManagerUserId(managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([employee]);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheet);

        var sut = CreateSut();
        var result = await sut.GetResourceTimesheetDetail(managerUserId, employee.Id, weekStart);

        Assert.Equal(employee.Id, result.ResourceUserId);
        Assert.Equal(TimesheetConstants.StatusSubmitted, result.Status);
        Assert.Equal(20, result.TotalHours);
    }

    [Fact]
    public async Task GetResourceTimesheetDetail_WhenResourceNotOnTeam_ThrowsUnauthorizedAccessException()
    {
        const int managerUserId = 10;
        var employee = ApiTestData.CreateResourceUser(managerUserId: 99);

        _userRepository
            .Setup(x => x.GetResourceUsersByManagerUserId(managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.GetResourceTimesheetDetail(managerUserId, employee.Id, weekStart));

        Assert.Equal(AppConstants.Timesheets.ResourceNotOnTeam, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenDuplicateProjectInEntries_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        SetupEmployeeByUserId(employee);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 4, ActivityTagIds = [1] },
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 4, ActivityTagIds = [1] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.DuplicateProjectInEntries, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenHoursExceedAllocation_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            projectId: 1,
            utilizationPercent: 50,
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);
        SetupOverlappingAllocations(employee.Id, [allocation]);

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 25, ActivityTagIds = [1] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.HoursExceedAllocation, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenInvalidActivityTag_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            projectId: 1,
            utilizationPercent: 50,
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);
        SetupOverlappingAllocations(employee.Id, [allocation]);
        _timesheetRepository
            .Setup(x => x.GetActivityTagsByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ActivityTag>());

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [999] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.InvalidActivityTag, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenInvalidWeekStart_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var invalidWeekStart = weekStart.AddDays(1);

        SetupEmployeeByUserId(employee);

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = invalidWeekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [1] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.InvalidWeekStart, exception.Message);
    }

    [Fact]
    public async Task GetWeekAllocations_WhenValid_ReturnsAllocationsWithMaxHours()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            projectId: 1,
            utilizationPercent: 50,
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupOverlappingAllocations(employee.Id, [allocation]);

        var sut = CreateSut();
        var result = await sut.GetWeekAllocations(employee.Id, weekStart);

        Assert.Equal(employee.FullName, result.ResourceName);
        Assert.Equal(40, result.MaxWeeklyHours);
        Assert.Single(result.Allocations);
        Assert.Equal(20, result.Allocations[0].MaxHours);
    }

    [Fact]
    public async Task GetResourceTimesheetDetail_WhenMissedWeekWithAllocation_ReturnsMissedStatus()
    {
        const int managerUserId = 10;
        var employee = ApiTestData.CreateResourceUser(managerUserId: managerUserId);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));

        _userRepository
            .Setup(x => x.GetResourceUsersByManagerUserId(managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([employee]);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Timesheet?)null);
        _allocationRepository
            .Setup(x => x.GetOverlappingForUser(
                It.Is<UserAllocationPeriodQuery>(query => query.UserId == employee.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([allocation]);

        var sut = CreateSut();
        var result = await sut.GetResourceTimesheetDetail(managerUserId, employee.Id, weekStart);

        Assert.Equal(TimesheetConstants.StatusMissed, result.Status);
        Assert.Equal(0, result.TotalHours);
    }

    [Fact]
    public async Task GetResourceTimesheetDetail_WhenNoAllocationAndNoTimesheet_ThrowsKeyNotFoundException()
    {
        const int managerUserId = 10;
        var employee = ApiTestData.CreateResourceUser(managerUserId: managerUserId);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        _userRepository
            .Setup(x => x.GetResourceUsersByManagerUserId(managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([employee]);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Timesheet?)null);
        _allocationRepository
            .Setup(x => x.GetOverlappingForUser(
                It.Is<UserAllocationPeriodQuery>(query => query.UserId == employee.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Allocation>());

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetResourceTimesheetDetail(managerUserId, employee.Id, weekStart));

        Assert.Equal(AppConstants.Timesheets.NotFound, exception.Message);
    }

    [Fact]
    public async Task GetTeamTimesheets_IncludesMissedEmployeesWithAllocations()
    {
        const int managerUserId = 10;
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        var employee = ApiTestData.CreateResourceUser();
        var allocation = ApiTestData.CreateAllocation(
            userId: employee.Id,
            fromDate: weekStart,
            toDate: weekEnd);

        _timesheetRepository
            .Setup(x => x.GetEntriesForTeamByManagerAndWeek(managerUserId, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TeamTimesheetEntryRow>());
        _userRepository
            .Setup(x => x.GetResourceUsersByManagerUserId(managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([employee]);
        _allocationRepository
            .Setup(x => x.GetOverlappingForUser(
                It.Is<UserAllocationPeriodQuery>(query =>
                    query.UserId == employee.Id
                    && query.FromDate == weekStart
                    && query.ToDate == weekEnd),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([allocation]);

        var sut = CreateSut();
        var result = await sut.GetTeamTimesheets(managerUserId, weekStart);

        Assert.Single(result.Rows);
        Assert.Equal(TimesheetConstants.StatusMissed, result.Rows[0].Status);
        Assert.Equal(employee.FullName, result.Rows[0].ResourceName);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenProjectNotAllocated_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);
        SetupOverlappingAllocations(employee.Id, Array.Empty<Allocation>());

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [1] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.ProjectNotAllocated, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenActivityTagsMissing_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            projectId: 1,
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);
        SetupOverlappingAllocations(employee.Id, [allocation]);

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.ActivityTagsRequired, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenTotalHoursExceedMax_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation1 = ApiTestData.CreateAllocation(id: 1, projectId: 1, utilizationPercent: 100, fromDate: weekStart, toDate: weekStart.AddDays(6));
        var allocation2 = ApiTestData.CreateAllocation(id: 2, projectId: 2, projectName: "Beta", utilizationPercent: 100, fromDate: weekStart, toDate: weekStart.AddDays(6));
        var tags = ApiTestData.CreateStandardActivityTags();

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);
        SetupOverlappingAllocations(employee.Id, [allocation1, allocation2]);
        _timesheetRepository
            .Setup(x => x.GetActivityTagsByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                tags.Where(tag => ids.Contains(tag.Id)).ToList());

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 25, ActivityTagIds = [tags[0].Id] },
                new TimesheetEntryRequest { ProjectId = 2, HoursWorked = 25, ActivityTagIds = [tags[0].Id] },
            ],
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.TotalHoursExceedMax, exception.Message);
    }

    [Fact]
    public async Task SubmitTimesheet_WithOtherActivityTag_UsesFindOrCreateActivityTag()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            projectId: 1,
            utilizationPercent: 50,
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));
        var customTag = ApiTestData.CreateActivityTag(id: 50, name: "Custom Work");

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);
        SetupOverlappingAllocations(employee.Id, [allocation]);
        _timesheetRepository
            .Setup(x => x.FindOrCreateActivityTagByName("Custom Work", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customTag);
        _timesheetRepository
            .Setup(x => x.Add(It.IsAny<Timesheet>(), It.IsAny<CancellationToken>()))
            .Callback<Timesheet, CancellationToken>((timesheet, _) => timesheet.Id = 101)
            .Returns(Task.CompletedTask);
        _timesheetRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.SubmitTimesheet(
            employee.Id,
            new SubmitTimesheetRequest
            {
                WeekStart = weekStart,
                Entries =
                [
                    new TimesheetEntryRequest
                    {
                        ProjectId = 1,
                        HoursWorked = 8,
                        OtherActivityTags = ["Custom Work"],
                    },
                ],
            });

        Assert.Equal(101, result.TimesheetId);
        _timesheetRepository.Verify(
            x => x.FindOrCreateActivityTagByName("Custom Work", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitTimesheet_WithStandardActivityTagNames_ResolvesTagIds()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            projectId: 1,
            utilizationPercent: 50,
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));
        var tags = ApiTestData.CreateStandardActivityTags();

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupSubmitWeek(employee.Id, weekStart, submitted: false);
        SetupOverlappingAllocations(employee.Id, [allocation]);
        _timesheetRepository
            .Setup(x => x.GetAllActivityTags(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);
        _timesheetRepository
            .Setup(x => x.GetActivityTagsByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                tags.Where(tag => ids.Contains(tag.Id)).ToList());
        _timesheetRepository
            .Setup(x => x.Add(It.IsAny<Timesheet>(), It.IsAny<CancellationToken>()))
            .Callback<Timesheet, CancellationToken>((timesheet, _) => timesheet.Id = 102)
            .Returns(Task.CompletedTask);
        _timesheetRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.SubmitTimesheet(
            employee.Id,
            new SubmitTimesheetRequest
            {
                WeekStart = weekStart,
                Entries =
                [
                    new TimesheetEntryRequest
                    {
                        ProjectId = 1,
                        HoursWorked = 8,
                        ActivityTags = [tags[0].Name],
                    },
                ],
            });

        Assert.Equal(102, result.TimesheetId);
    }

    [Fact]
    public async Task GetMyTimesheetDetail_WhenNoTimesheetOrAllocation_ThrowsKeyNotFoundException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        SetupEmployeeByUserId(employee);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Timesheet?)null);
        SetupOverlappingAllocations(employee.Id, Array.Empty<Allocation>());

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetMyTimesheetDetail(employee.Id, weekStart));

        Assert.Equal(AppConstants.Timesheets.NotFound, exception.Message);
    }

    [Fact]
    public async Task GetMyTimesheets_IncludesMissedWeeksInHistory()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));

        SetupEmployeeByUserId(employee);
        _timesheetRepository
            .Setup(x => x.GetByUserId(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Timesheet>());
        SetupOverlappingAllocations(employee.Id, [allocation]);

        var sut = CreateSut();
        var result = await sut.GetMyTimesheets(employee.Id);

        Assert.Contains(result.Timesheets, row => row.Status == TimesheetConstants.StatusMissed);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenAccessBlocked_ThrowsInvalidOperationException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        SetupEmployeeByUserId(employee);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Timesheet
            {
                Id = 50,
                UserId = employee.Id,
                WeekStart = weekStart,
                TotalHours = 0,
                Status = TimesheetConstants.StatusMissed,
                Access = TimesheetConstants.AccessBlocked,
            });

        var sut = CreateSut();
        var request = new SubmitTimesheetRequest
        {
            WeekStart = weekStart,
            Entries =
            [
                new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [1] },
            ],
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitTimesheet(employee.Id, request));

        Assert.Equal(AppConstants.Timesheets.AccessBlocked, exception.Message);
    }

    [Fact]
    public async Task GetWeekAllocations_WhenAccessBlocked_ThrowsInvalidOperationException()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        SetupEmployeeByUserId(employee);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Timesheet
            {
                Id = 51,
                UserId = employee.Id,
                WeekStart = weekStart,
                TotalHours = 0,
                Status = TimesheetConstants.StatusMissed,
                Access = TimesheetConstants.AccessBlocked,
            });

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetWeekAllocations(employee.Id, weekStart));

        Assert.Equal(AppConstants.Timesheets.AccessBlocked, exception.Message);
    }

    [Fact]
    public async Task AllowTimesheetAccess_WhenBlocked_RestoresAccess()
    {
        var managerUserId = 10;
        var employee = ApiTestData.CreateResourceUser(managerUserId: managerUserId);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var blockedTimesheet = new Timesheet
        {
            Id = 52,
            UserId = employee.Id,
            WeekStart = weekStart,
            TotalHours = 0,
            Status = TimesheetConstants.StatusMissed,
            Access = TimesheetConstants.AccessBlocked,
        };

        _userRepository
            .Setup(x => x.IsResourceManagedByManager(employee.Id, managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedTimesheet);
        _timesheetRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AllowTimesheetAccess(managerUserId, employee.Id, weekStart);

        Assert.Equal(TimesheetConstants.AccessAllowed, result.Access);
        Assert.Equal(TimesheetConstants.AccessAllowed, blockedTimesheet.Access);
        _timesheetRepository.Verify(x => x.Update(blockedTimesheet), Times.Once);
    }

    [Fact]
    public async Task SubmitTimesheet_WhenBlockedThenAllowed_UpdatesExistingTimesheet()
    {
        var employee = ApiTestData.CreateResourceUser();
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var allocation = ApiTestData.CreateAllocation(
            projectId: 1,
            utilizationPercent: 50,
            fromDate: weekStart,
            toDate: weekStart.AddDays(6));
        var tags = ApiTestData.CreateStandardActivityTags();
        var unlockedTimesheet = new Timesheet
        {
            Id = 53,
            UserId = employee.Id,
            WeekStart = weekStart,
            TotalHours = 0,
            Status = TimesheetConstants.StatusMissed,
            Access = TimesheetConstants.AccessAllowed,
            Entries = [],
        };

        SetupEmployeeByUserId(employee);
        SetupMaxWeeklyHours(40);
        SetupOverlappingAllocations(employee.Id, [allocation]);
        _timesheetRepository
            .Setup(x => x.IsSubmittedForUserWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(employee.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unlockedTimesheet);
        _timesheetRepository
            .Setup(x => x.GetActivityTagsByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                tags.Where(tag => ids.Contains(tag.Id)).ToList());
        _timesheetRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.SubmitTimesheet(
            employee.Id,
            new SubmitTimesheetRequest
            {
                WeekStart = weekStart,
                Entries =
                [
                    new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [tags[0].Id] },
                ],
            });

        Assert.Equal(53, result.TimesheetId);
        Assert.Equal(TimesheetConstants.StatusSubmitted, unlockedTimesheet.Status);
        Assert.Equal(TimesheetConstants.AccessAllowed, unlockedTimesheet.Access);
        _timesheetRepository.Verify(x => x.Update(unlockedTimesheet), Times.Once);
        _timesheetRepository.Verify(x => x.Add(It.IsAny<Timesheet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private TimesheetService CreateSut() =>
        new(
            _timesheetRepository.Object,
            _userRepository.Object,
            _allocationRepository.Object,
            _systemConfigurationRepository.Object);

    private void SetupEmployeeByUserId(User user)
    {
        _userRepository
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    private void SetupMaxWeeklyHours(int hours)
    {
        _systemConfigurationRepository
            .Setup(x => x.GetById((int)ConfigurationOptionEnum.MaxWeeklyHours, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.MaxWeeklyHours, hours.ToString()));
    }

    private void SetupOverlappingAllocations(int resourceUserId, IReadOnlyList<Allocation> allocations)
    {
        _allocationRepository
            .Setup(x => x.GetOverlappingForUser(
                It.Is<UserAllocationPeriodQuery>(query => query.UserId == resourceUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);
    }

    private void SetupSubmitWeek(int resourceUserId, DateOnly weekStart, bool submitted, Timesheet? existing = null)
    {
        _timesheetRepository
            .Setup(x => x.IsSubmittedForUserWeek(resourceUserId, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submitted);

        if (existing is not null)
        {
            _timesheetRepository
                .Setup(x => x.GetByUserAndWeek(resourceUserId, weekStart, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            return;
        }

        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(resourceUserId, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submitted
                ? new Timesheet
                {
                    Id = 1,
                    UserId = resourceUserId,
                    WeekStart = weekStart,
                    TotalHours = 8,
                    Status = TimesheetConstants.StatusSubmitted,
                    Access = TimesheetConstants.AccessAllowed,
                }
                : null);
    }
}
