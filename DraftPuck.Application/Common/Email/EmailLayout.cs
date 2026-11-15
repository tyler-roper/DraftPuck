using System.Net;

namespace DraftPuck.Application.Common.Email;

/// <summary>What goes into a <see cref="EmailLayout"/>. All text is plain; the layout HTML-encodes it.</summary>
public record EmailLayoutContent(
    string Preheader,
    string Heading,
    IReadOnlyList<string> Paragraphs,
    string ButtonText,
    string ButtonUrl,
    string Footnote);

/// <summary>
/// The branded HTML shell for DraftPuck's emails: the yellow logo header, a white card, one button, and a footer.
/// Built from tables with inline styles, which is what Gmail, Outlook and Apple Mail all render the same way.
/// </summary>
public static class EmailLayout
{
    /// <summary>The logo's background yellow, so the header and the logo image blend into one band.</summary>
    private const string Yellow = "#FFCE00";
    private const string Ink = "#18181B";
    private const string Body = "#3F3F46";
    private const string Muted = "#71717A";

    public static string Render(EmailLayoutContent content, ApplicationOptions app)
    {
        static string E(string text) => WebUtility.HtmlEncode(text);

        var logoUrl = app.GetAbsoluteUrl("img/icons/icon-512.png");
        var siteUrl = app.GetAbsoluteUrl("");
        var paragraphs = string.Concat(content.Paragraphs.Select(p =>
            $"""<p style="margin: 0 0 16px; font-size: 16px; line-height: 1.6; color: {Body};">{E(p)}</p>"""));

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="light">
              <meta name="supported-color-schemes" content="light">
              <title>{E(content.Heading)}</title>
            </head>
            <body style="margin: 0; padding: 0; background-color: #F4F4F5;">
              <div style="display: none; max-height: 0; overflow: hidden; opacity: 0; color: transparent;">{E(content.Preheader)}</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color: #F4F4F5;">
                <tr>
                  <td align="center" style="padding: 32px 16px;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width: 560px; background-color: #FFFFFF; border-radius: 12px; overflow: hidden; font-family: 'Segoe UI', Helvetica, Arial, sans-serif;">
                      <tr>
                        <td align="center" style="background-color: {Yellow}; padding: 8px 0;">
                          <a href="{E(siteUrl)}"><img src="{E(logoUrl)}" width="160" height="160" alt="DraftPuck" style="display: block; border: 0; width: 160px; height: 160px;"></a>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding: 36px 40px 8px;">
                          <h1 style="margin: 0 0 20px; font-size: 24px; line-height: 1.3; font-weight: 700; color: {Ink};">{E(content.Heading)}</h1>
                          {paragraphs}
                        </td>
                      </tr>
                      <tr>
                        <td align="center" style="padding: 8px 40px 28px;">
                          <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td align="center" style="background-color: {Ink}; border-radius: 8px;">
                                <a href="{E(content.ButtonUrl)}" style="display: inline-block; padding: 14px 32px; font-size: 16px; font-weight: 700; color: {Yellow}; text-decoration: none; border-radius: 8px;">{E(content.ButtonText)}</a>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding: 0 40px 32px;">
                          <p style="margin: 0 0 6px; font-size: 13px; line-height: 1.5; color: {Muted};">Button not working? Paste this link into your browser:</p>
                          <p style="margin: 0 0 24px; font-size: 13px; line-height: 1.5; word-break: break-all;"><a href="{E(content.ButtonUrl)}" style="color: {Ink};">{E(content.ButtonUrl)}</a></p>
                          <p style="margin: 0; padding-top: 20px; border-top: 1px solid #E4E4E7; font-size: 13px; line-height: 1.5; color: {Muted};">{E(content.Footnote)}</p>
                        </td>
                      </tr>
                    </table>
                    <p style="margin: 20px 0 0; font-family: 'Segoe UI', Helvetica, Arial, sans-serif; font-size: 12px; line-height: 1.5; color: {Muted};">
                      <a href="{E(siteUrl)}" style="color: {Muted}; font-weight: 700; text-decoration: none;">DraftPuck</a> &middot; A live hockey drinking game.
                    </p>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
