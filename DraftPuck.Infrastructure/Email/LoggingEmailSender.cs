using Microsoft.Extensions.Logging;

namespace DraftPuck.Infrastructure.Email;

/// <summary>
/// Used when no email connection string is configured (local development): writes the email to the log instead of
/// sending it, so a reset link can be copied from the console.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogWarning("Email isn't configured, so this email was logged instead of sent.\nTo: {To}\nSubject: {Subject}\n\n{Body}",
            message.To, message.Subject, message.PlainText);
        return Task.CompletedTask;
    }
}
