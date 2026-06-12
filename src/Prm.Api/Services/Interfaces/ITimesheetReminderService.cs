namespace Prm.Api.Services.Interfaces;

public interface ITimesheetReminderService
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
