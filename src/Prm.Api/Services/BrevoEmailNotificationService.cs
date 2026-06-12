using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Prm.Api.Configuration;
using Prm.Api.Models.Email;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;

namespace Prm.Api.Services;

public class BrevoEmailNotificationService(
    IOptions<BrevoOptions> options,
    ILogger<BrevoEmailNotificationService> logger) : IEmailNotificationService
{
    private readonly BrevoOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_options.Enabled)
        {
            logger.LogInformation(
                "Email notifications are disabled. Skipping send to {RecipientEmail} with subject {Subject}.",
                message.ToEmail,
                message.Subject);
            return;
        }

        var mimeMessage = BuildMimeMessage(message);

        using var smtpClient = new SmtpClient();
        try
        {
            await smtpClient.ConnectAsync(
                _options.SmtpHost,
                _options.SmtpPort,
                GetSecureSocketOptions(_options.SmtpPort),
                cancellationToken);
            await smtpClient.AuthenticateAsync(_options.SmtpLogin, _options.SmtpKey, cancellationToken);
            await smtpClient.SendAsync(mimeMessage, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);

            logger.LogInformation(
                "Email sent via Brevo SMTP to {RecipientEmail} with subject {Subject}.",
                message.ToEmail,
                message.Subject);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Brevo SMTP failed while sending email to {RecipientEmail}.",
                message.ToEmail);
            throw new InvalidOperationException(AppConstants.Email.SendFailed, ex);
        }
    }

    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        mimeMessage.To.Add(new MailboxAddress(message.ToName ?? message.ToEmail, message.ToEmail));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        };

        mimeMessage.Body = bodyBuilder.ToMessageBody();
        return mimeMessage;
    }

    private static SecureSocketOptions GetSecureSocketOptions(int smtpPort) =>
        smtpPort switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.StartTls,
        };
}
