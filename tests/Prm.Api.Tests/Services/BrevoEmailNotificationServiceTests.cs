using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Prm.Api.Configuration;
using Prm.Api.Models.Email;
using Prm.Api.Services;
using Prm.Common.Constants;

namespace Prm.Api.Tests.Services;

public class BrevoEmailNotificationServiceTests
{
    [Fact]
    public async Task SendAsync_WhenDisabled_CompletesWithoutSending()
    {
        var sut = CreateSut(new BrevoOptions
        {
            Enabled = false,
            SenderEmail = "noreply@prm.local",
        });

        await sut.SendAsync(new EmailMessage
        {
            ToEmail = "user@example.com",
            Subject = "Test",
            HtmlBody = "<p>Hello</p>",
        });
    }

    [Fact]
    public async Task SendAsync_WhenEnabledAndSmtpUnavailable_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(new BrevoOptions
        {
            Enabled = true,
            SmtpLogin = "smtp-login@brevo.com",
            SmtpKey = "smtp-key",
            SmtpHost = "127.0.0.1",
            SmtpPort = 1,
            SenderEmail = "noreply@prm.local",
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SendAsync(new EmailMessage
            {
                ToEmail = "user@example.com",
                Subject = "Test",
                HtmlBody = "<p>Hello</p>",
            }));

        Assert.Equal(AppConstants.Email.SendFailed, exception.Message);
    }

    private static BrevoEmailNotificationService CreateSut(BrevoOptions options) =>
        new(
            Options.Create(options),
            NullLogger<BrevoEmailNotificationService>.Instance);
}
