using System.Globalization;
using System.Text.RegularExpressions;

namespace DraftPuck.Shared.Utils;

public static partial class RegexUtilities
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Normalize the domain
            email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                  RegexOptions.None, TimeSpan.FromMilliseconds(200));

            // Examines the domain part of the email and normalizes it.
            static string DomainMapper(Match match)
            {
                // Use IdnMapping class to convert Unicode domain names.
                var idn = new IdnMapping();

                // Pull out and process domain name (throws ArgumentException on invalid)
                var domainName = idn.GetAscii(match.Groups[2].Value);

                return match.Groups[1].Value + domainName;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static bool IsValidNickname(string? nickname)
    {
        return !string.IsNullOrWhiteSpace(nickname) && nickname.Length is not > 50 and not < 1 && NicknameRegex().IsMatch(nickname);
    }

    /*
    Allowed characters: letters, numbers, spaces, apostrophes ('), and underscores (_)
    Must start with:    a letter or number
    */
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9 '_]*$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NicknameRegex();
}

