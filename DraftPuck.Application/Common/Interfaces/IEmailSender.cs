namespace DraftPuck.Application.Common.Interfaces;

public record EmailMessage(string To, string Subject, string PlainText, string Html);

/// <summary>Sends transactional email (password resets). Implemented in Infrastructure with Azure Communication Services.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
