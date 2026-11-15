using DraftPuck.Application.Common.Email;

namespace DraftPuck.Application.Features.Auth.PasswordReset;

public static class PasswordResetEmail
{
    public const string Subject = "Reset your DraftPuck password";

    public static EmailMessage Create(string to, string nickname, string link, ApplicationOptions app)
    {
        var minutes = (int)PasswordResetTokens.Lifetime.TotalMinutes;
        const string intro = "We got a request to reset the password for your DraftPuck account. Choose a new one with the button below.";
        var expiry = $"The link works once, for the next {minutes} minutes.";
        const string footnote = "Didn't ask for this? You can ignore this email. Your password stays the same until someone uses the link.";

        var plainText =
            $"""
            Hi {nickname},

            {intro}

            {link}

            {expiry}

            {footnote}

            DraftPuck - A live hockey drinking game.
            """;

        var html = EmailLayout.Render(new EmailLayoutContent(
            Preheader: $"Choose a new password. The link works for {minutes} minutes.",
            Heading: $"Hi {nickname}, let's get you back in the game",
            Paragraphs: [intro, expiry],
            ButtonText: "Choose a new password",
            ButtonUrl: link,
            Footnote: footnote), app);

        return new EmailMessage(to, Subject, plainText, html);
    }
}
