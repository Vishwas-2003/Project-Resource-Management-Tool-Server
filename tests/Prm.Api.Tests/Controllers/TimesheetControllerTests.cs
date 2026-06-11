using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;
using Prm.Common.Models.Timesheets;
using Prm.Common.Models.Users;

namespace Prm.Api.Tests.Controllers;

public class TimesheetControllerTests
{
    private readonly Mock<ITimesheetService> _timesheetService = new();
    private const int EmployeeUserId = 1;
    private const int ManagerUserId = 10;

    [Fact]
    public async Task GetActivityTags_WhenTagsExist_ReturnsOk()
    {
        var response = new ActivityTagsResponse
        {
            Tags = [new ActivityTagOption { RowNumber = 1, Name = "Development" }],
        };

        _timesheetService
            .Setup(x => x.GetActivityTags(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(EmployeeUserId, RoleNameEnum.Employee).GetActivityTags(CancellationToken.None);
        Assert.Single(ControllerTestHelper.AssertOkValue<ActivityTagsResponse>(result).Tags);
    }

    [Fact]
    public async Task GetReminder_WhenMissing_ReturnsOk()
    {
        var response = new MissingTimesheetReminder { HasMissing = true, WeekStart = new DateOnly(2026, 5, 26) };

        _timesheetService
            .Setup(x => x.GetMissingReminder(EmployeeUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(EmployeeUserId, RoleNameEnum.Employee).GetReminder(CancellationToken.None);
        Assert.True(ControllerTestHelper.AssertOkValue<MissingTimesheetReminder>(result).HasMissing);
    }

    [Fact]
    public async Task GetWeekAllocations_WhenValid_ReturnsOk()
    {
        var weekStart = new DateOnly(2026, 5, 26);
        var response = new WeekAllocationsResponse { WeekStart = weekStart, EmployeeName = "Jane Doe" };

        _timesheetService
            .Setup(x => x.GetWeekAllocations(EmployeeUserId, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(EmployeeUserId, RoleNameEnum.Employee)
            .GetWeekAllocations(weekStart, CancellationToken.None);

        Assert.Equal("Jane Doe", ControllerTestHelper.AssertOkValue<WeekAllocationsResponse>(result).EmployeeName);
    }

    [Fact]
    public async Task Submit_WhenValid_ReturnsCreated()
    {
        var response = new SubmitTimesheetResponse { TimesheetId = 1, TotalHours = 8 };

        _timesheetService
            .Setup(x => x.SubmitTimesheet(EmployeeUserId, It.IsAny<SubmitTimesheetRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(EmployeeUserId, RoleNameEnum.Employee).Submit(
            new SubmitTimesheetRequest
            {
                WeekStart = new DateOnly(2026, 5, 26),
                Entries = [new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [1] }],
            },
            CancellationToken.None);

        Assert.Equal(1, ControllerTestHelper.AssertCreatedValue<SubmitTimesheetResponse>(result).TimesheetId);
    }

    [Fact]
    public async Task Submit_WhenAlreadySubmitted_Returns400()
    {
        _timesheetService
            .Setup(x => x.SubmitTimesheet(EmployeeUserId, It.IsAny<SubmitTimesheetRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(AppConstants.Timesheets.AlreadySubmitted));

        var result = await CreateSut(EmployeeUserId, RoleNameEnum.Employee).Submit(
            new SubmitTimesheetRequest
            {
                WeekStart = new DateOnly(2026, 5, 26),
                Entries = [new TimesheetEntryRequest { ProjectId = 1, HoursWorked = 8, ActivityTagIds = [1] }],
            },
            CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status400BadRequest,
            AppConstants.ErrorCodes.BadRequest);
    }

    [Fact]
    public async Task GetMyTimesheets_WhenValid_ReturnsOk()
    {
        var response = new MyTimesheetsResponse
        {
            Timesheets = [new MyTimesheetRow { RowNumber = 1, TotalHours = 32 }],
        };

        _timesheetService
            .Setup(x => x.GetMyTimesheets(EmployeeUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(EmployeeUserId, RoleNameEnum.Employee).GetMyTimesheets(CancellationToken.None);
        Assert.Single(ControllerTestHelper.AssertOkValue<MyTimesheetsResponse>(result).Timesheets);
    }

    [Fact]
    public async Task GetMyTimesheetDetail_WhenValid_ReturnsOk()
    {
        var weekStart = new DateOnly(2026, 5, 26);
        var response = new TimesheetWeekDetailResponse { WeekStart = weekStart, TotalHours = 16 };

        _timesheetService
            .Setup(x => x.GetMyTimesheetDetail(EmployeeUserId, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(EmployeeUserId, RoleNameEnum.Employee)
            .GetMyTimesheetDetail(weekStart, CancellationToken.None);

        Assert.Equal(16, ControllerTestHelper.AssertOkValue<TimesheetWeekDetailResponse>(result).TotalHours);
    }

    [Fact]
    public async Task GetMyAllocations_WhenValid_ReturnsOk()
    {
        var response = new EmployeeAllocationsResponse { TotalUtilizationPercent = 100 };

        _timesheetService
            .Setup(x => x.GetMyAllocations(EmployeeUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(EmployeeUserId, RoleNameEnum.Employee).GetMyAllocations(CancellationToken.None);
        Assert.Equal(100, ControllerTestHelper.AssertOkValue<EmployeeAllocationsResponse>(result).TotalUtilizationPercent);
    }

    [Fact]
    public async Task GetTeamTimesheets_WhenValid_ReturnsOk()
    {
        var weekStart = new DateOnly(2026, 5, 26);
        var response = new TeamTimesheetsResponse { WeekStart = weekStart };

        _timesheetService
            .Setup(x => x.GetTeamTimesheets(ManagerUserId, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(ManagerUserId, RoleNameEnum.Manager)
            .GetTeamTimesheets(weekStart, CancellationToken.None);

        Assert.Equal(weekStart, ControllerTestHelper.AssertOkValue<TeamTimesheetsResponse>(result).WeekStart);
    }

    [Fact]
    public async Task GetEmployeeTimesheetDetail_WhenValid_ReturnsOk()
    {
        var weekStart = new DateOnly(2026, 5, 26);
        var response = new EmployeeTimesheetDetailResponse { EmployeeUserId = 1, TotalHours = 20 };

        _timesheetService
            .Setup(x => x.GetEmployeeTimesheetDetail(ManagerUserId, 1, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut(ManagerUserId, RoleNameEnum.Manager)
            .GetEmployeeTimesheetDetail(1, weekStart, CancellationToken.None);

        Assert.Equal(20, ControllerTestHelper.AssertOkValue<EmployeeTimesheetDetailResponse>(result).TotalHours);
    }

    private TimesheetController CreateSut(int userId, RoleNameEnum role) =>
        new(
            _timesheetService.Object,
            ControllerTestHelper.CreateManagerAccess(
                userId,
                role == RoleNameEnum.Manager
                    ? ApiTestData.CreateUser(userId, (int)RoleNameEnum.Manager)
                    : ApiTestData.CreateUser(userId, (int)RoleNameEnum.Employee)));
}
