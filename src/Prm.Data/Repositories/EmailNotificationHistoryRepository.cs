using Microsoft.EntityFrameworkCore;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class EmailNotificationHistoryRepository(AppDbContext _dbContext)
    : CrudBaseRepository<EmailNotificationHistory, int>(_dbContext), IEmailNotificationHistoryRepository
{
    public Task<bool> ExistsForProjectRiskOnDateAsync(
        int projectId,
        DateOnly sentOnDate,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(
            x => x.EmailTypeId == (int)EmailNotificationTypeEnum.ProjectRisk
                && x.ProjectId == projectId
                && x.SentOnDate == sentOnDate,
            cancellationToken);

    public Task<bool> ExistsForMissedTimesheetOnDateAsync(
        int userId,
        DateOnly sentOnDate,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(
            x => x.EmailTypeId == (int)EmailNotificationTypeEnum.MissedTimeSheet
                && x.UserId == userId
                && x.SentOnDate == sentOnDate,
            cancellationToken);
}
