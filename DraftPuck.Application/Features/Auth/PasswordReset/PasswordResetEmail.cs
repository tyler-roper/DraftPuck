using System.Net;

namespace DraftPuck.Application.Features.Auth.PasswordReset;

public static class PasswordResetEmail
{
    public const string Subject = "Reset your DraftPuck password";

    public static EmailMessage Create(string to, string nickname, string link)
    {
        var minutes = (int)PasswordResetTokens.Lifetime.TotalMinutes;

        var plainText =
            $"""
            Hi {nickname},

            Someone (hopefully you) asked to reset the password for your DraftPuck account.
            Open this link to choose a new one. It works once, for the next {minutes} minutes:

            {link}

            If you didn't ask for this, you can ignore this email; your password hasn't changed.
            """;

        var html =
            $"""
            <div style="font-family: Arial, sans-serif; font-size: 15px; color: #222; max-width: 480px;">
              <p>Hi {WebUtility.HtmlEncode(nickname)},</p>
              <p>Someone (hopefully you) asked to reset the password for your DraftPuck account.
                 This link works once, for the next {minutes} minutes.</p>
              <p style="margin: 24px 0;">
                <a href="{WebUtility.HtmlEncode(link)}"
                   style="background: #222; color: #fff; padding: 12px 20px; border-radius: 6px; text-decoration: none;">Choose a new password</a>
              </p>
              <p style="color: #666; font-size: 13px;">If you didn't ask for this, you can ignore this email; your password hasn't changed.</p>
            </div>
            """;

        return new EmailMessage(to, Subject, plainText, html);
    }
}
