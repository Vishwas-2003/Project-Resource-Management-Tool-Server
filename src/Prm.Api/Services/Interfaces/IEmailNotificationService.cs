using Prm.Api.Models.Email;

namespace Prm.Api.Services.Interfaces;

public interface IEmailNotificationService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
