using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prm.Api.Models.Email;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Resources;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class TimesheetReminderServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAllocationRepository> _allocationRepository = new();
    private readonly Mock<ITimesheetRepository> _timesheetRepository = new();
    private readonly Mock<IEmailNotificationHistoryRepository> _emailNotificationHistoryRepository = new();
    private readonly Mock<IEmailNotificationService> _emailNotificationService = new();

    public TimesheetReminderServiceTests()
    {
        _emailNotificationHistoryRepository
            .Setup(x => x.ExistsForMissedTimesheetOnDateAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _emailNotificationHistoryRepository
            .Setup(x => x.Add(It.IsAny<EmailNotificationHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailNotificationHistoryRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task ExecuteAsync_OnThursday_DoesNotSendEmail()
    {
        var sut = CreateSut(new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc));

        await sut.ExecuteAsync();

        _emailNotificationService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userRepository.Verify(
            x => x.GetResourceUsers(It.IsAny<ResourceFilter>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OnMonday_SendsReminderEmail()
    {
        var monday = new DateTime(2026, 6, 8, 9, 0, 0, DateTimeKind.Utc);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(monday));
        var resource = ApiTestData.CreateResourceUser();
        var allocation = ApiTestData.CreateAllocation(fromDate: weekStart, toDate: weekStart.AddDays(6));

        SetupMissingResource(resource, weekStart, allocation, submitted: false);

        EmailMessage? capturedEmail = null;
        _emailNotificationService
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => capturedEmail = message)
            .Returns(Task.CompletedTask);

        var sut = CreateSut(monday);
        await sut.ExecuteAsync();

        Assert.NotNull(capturedEmail);
        Assert.Equal(resource.Email, capturedEmail.ToEmail);
        Assert.Contains(weekStart.ToString("yyyy-MM-dd"), capturedEmail.Subject);
        _emailNotificationHistoryRepository.Verify(
            x => x.Add(
                It.Is<EmailNotificationHistory>(history =>
                    history.EmailTypeId == (int)EmailNotificationTypeEnum.MissedTimeSheet
                    && history.UserId == resource.Id
                    && history.EntityId == 100),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OnTuesday_SendsWarningEmail()
    {
        var tuesday = new DateTime(2026, 6, 9, 9, 0, 0, DateTimeKind.Utc);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(tuesday));
        var resource = ApiTestData.CreateResourceUser();
        var allocation = ApiTestData.CreateAllocation(fromDate: weekStart, toDate: weekStart.AddDays(6));

        SetupMissingResource(resource, weekStart, allocation, submitted: false);

        EmailMessage? capturedEmail = null;
        _emailNotificationService
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => capturedEmail = message)
            .Returns(Task.CompletedTask);

        var sut = CreateSut(tuesday);
        await sut.ExecuteAsync();

        Assert.NotNull(capturedEmail);
        Assert.Contains("blocked", capturedEmail.HtmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_OnWednesday_BlocksAndNotifiesResourceAndManager()
    {
        var wednesday = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(wednesday));
        var manager = ApiTestData.CreateManager();
        var resource = ApiTestData.CreateResourceUser(managerUserId: manager.Id);
        var allocation = ApiTestData.CreateAllocation(fromDate: weekStart, toDate: weekStart.AddDays(6));

        SetupMissingResource(resource, weekStart, allocation, submitted: false);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(resource.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Timesheet?)null);
        _timesheetRepository
            .Setup(x => x.EnsureBlockedTimesheetAsync(resource.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Timesheet
            {
                Id = 100,
                UserId = resource.Id,
                WeekStart = weekStart,
                Status = TimesheetConstants.StatusMissed,
                Access = TimesheetConstants.AccessBlocked,
                TotalHours = 0,
            });
        _userRepository
            .Setup(x => x.GetCurrentManagerForResourceUserId(resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        var sentEmails = new List<EmailMessage>();
        _emailNotificationService
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentEmails.Add(message))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(wednesday);
        await sut.ExecuteAsync();

        _timesheetRepository.Verify(x => x.EnsureBlockedTimesheetAsync(resource.Id, weekStart, It.IsAny<CancellationToken>()), Times.Once);
        _timesheetRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, sentEmails.Count);
        Assert.Contains(sentEmails, email => email.ToEmail == resource.Email);
        Assert.Contains(sentEmails, email => email.ToEmail == manager.Email);
        _emailNotificationHistoryRepository.Verify(
            x => x.Add(
                It.Is<EmailNotificationHistory>(history => history.EntityId == 100),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_OnWednesday_WhenAlreadyBlocked_SkipsBlockAndEmails()
    {
        var wednesday = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(wednesday));
        var resource = ApiTestData.CreateResourceUser();
        var allocation = ApiTestData.CreateAllocation(fromDate: weekStart, toDate: weekStart.AddDays(6));

        SetupMissingResource(resource, weekStart, allocation, submitted: false);
        _timesheetRepository
            .Setup(x => x.GetByUserAndWeek(resource.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Timesheet
            {
                UserId = resource.Id,
                WeekStart = weekStart,
                Status = TimesheetConstants.StatusMissed,
                Access = TimesheetConstants.AccessBlocked,
                TotalHours = 0,
            });

        var sut = CreateSut(wednesday);
        await sut.ExecuteAsync();

        _timesheetRepository.Verify(
            x => x.EnsureBlockedTimesheetAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailNotificationService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OnMonday_WhenEmailAlreadySentToday_SkipsEmail()
    {
        var monday = new DateTime(2026, 6, 8, 9, 0, 0, DateTimeKind.Utc);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(monday));
        var resource = ApiTestData.CreateResourceUser();
        var allocation = ApiTestData.CreateAllocation(fromDate: weekStart, toDate: weekStart.AddDays(6));

        SetupMissingResource(resource, weekStart, allocation, submitted: false);
        _emailNotificationHistoryRepository
            .Setup(x => x.ExistsForMissedTimesheetOnDateAsync(
                resource.Id,
                DateOnly.FromDateTime(monday),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(monday);
        await sut.ExecuteAsync();

        _emailNotificationService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailNotificationHistoryRepository.Verify(
            x => x.Add(It.IsAny<EmailNotificationHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimesheetSubmitted_SkipsResource()
    {
        var monday = new DateTime(2026, 6, 8, 9, 0, 0, DateTimeKind.Utc);
        var weekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(DateOnly.FromDateTime(monday));
        var resource = ApiTestData.CreateResourceUser();
        var allocation = ApiTestData.CreateAllocation(fromDate: weekStart, toDate: weekStart.AddDays(6));

        SetupMissingResource(resource, weekStart, allocation, submitted: true);

        var sut = CreateSut(monday);
        await sut.ExecuteAsync();

        _emailNotificationService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupMissingResource(
        User resource,
        DateOnly weekStart,
        Allocation allocation,
        bool submitted)
    {
        _userRepository
            .Setup(x => x.GetResourceUsers(
                It.Is<ResourceFilter>(filter => filter.IncludeInactive == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([resource]);
        _allocationRepository
            .Setup(x => x.GetOverlappingForUser(
                It.Is<UserAllocationPeriodQuery>(query =>
                    query.UserId == resource.Id
                    && query.FromDate == weekStart
                    && query.ToDate == weekStart.AddDays(6)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([allocation]);
        _timesheetRepository
            .Setup(x => x.IsSubmittedForUserWeek(resource.Id, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submitted);

        if (!submitted)
        {
            _timesheetRepository
                .Setup(x => x.GetOrEnsureMissedTimesheetAsync(resource.Id, weekStart, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Timesheet
                {
                    Id = 100,
                    UserId = resource.Id,
                    WeekStart = weekStart,
                    Status = TimesheetConstants.StatusMissed,
                    Access = TimesheetConstants.AccessAllowed,
                    TotalHours = 0,
                });
            _timesheetRepository
                .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }
    }

    private TimesheetReminderService CreateSut(DateTime utcNow) =>
        new(
            _userRepository.Object,
            _allocationRepository.Object,
            _timesheetRepository.Object,
            _emailNotificationHistoryRepository.Object,
            _emailNotificationService.Object,
            new FixedTimeProvider(utcNow),
            NullLogger<TimesheetReminderService>.Instance);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = new(utcNow, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
