namespace DraftPuck.Application.Features.Users;

/// <summary>Turns a User-Agent into a short, readable device description. Best effort: it's only for display.</summary>
public static class DeviceDescriptions
{
    public static string? Describe(string? userAgent, bool isInstalledApp)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return isInstalledApp ? "Installed app" : null;

        var platform = Platform(userAgent);
        var how = isInstalledApp ? "installed app" : Browser(userAgent);
        var description = platform == null ? how : $"{platform} · {how}";
        return description.Length > UserDeviceEntity.MaxDescriptionLength ? description[..UserDeviceEntity.MaxDescriptionLength] : description;
    }

    // Order matters: iPads and Android tablets also say "Macintosh" or "Linux", and every Chromium browser says "Chrome".
    private static string? Platform(string ua) =>
        Has(ua, "iPhone") ? "iPhone"
        : Has(ua, "iPad") ? "iPad"
        : Has(ua, "Android") ? "Android"
        : Has(ua, "Windows") ? "Windows"
        : Has(ua, "Macintosh") || Has(ua, "Mac OS X") ? "Mac"
        : Has(ua, "CrOS") ? "ChromeOS"
        : Has(ua, "Linux") ? "Linux"
        : null;

    private static string Browser(string ua) =>
        Has(ua, "Edg/") || Has(ua, "EdgA/") || Has(ua, "EdgiOS/") ? "Edge"
        : Has(ua, "SamsungBrowser") ? "Samsung Internet"
        : Has(ua, "Firefox/") || Has(ua, "FxiOS/") ? "Firefox"
        : Has(ua, "OPR/") ? "Opera"
        : Has(ua, "Chrome/") || Has(ua, "CriOS/") ? "Chrome"
        : Has(ua, "Safari/") ? "Safari"
        : "Browser";

    private static bool Has(string ua, string value) => ua.Contains(value, StringComparison.OrdinalIgnoreCase);
}
