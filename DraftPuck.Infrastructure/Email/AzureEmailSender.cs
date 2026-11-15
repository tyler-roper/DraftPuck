using Azure;
using Azure.Communication.Email;
using AcsEmailMessage = Azure.Communication.Email.EmailMessage;
using EmailMessage = DraftPuck.Application.Common.Interfaces.EmailMessage;

namespace DraftPuck.Infrastructure.Email;

/// <summary>Sends email through Azure Communication Services, from the verified draftpuck.com domain.</summary>
public class AzureEmailSender(EmailClient client, string senderAddress) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var email = new AcsEmailMessage(
            senderAddress,
            message.To,
            new EmailContent(message.Subject) { PlainText = message.PlainText, Html = message.Html });

        // Started: ACS has accepted the message. Delivery happens in the background, so the request doesn't wait for it.
        await client.SendAsync(WaitUntil.Started, email, ct);
    }
}
