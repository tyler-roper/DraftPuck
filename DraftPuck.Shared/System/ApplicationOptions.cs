namespace DraftPuck.Shared.System;

public class ApplicationOptions
{
    public const string SectionName = "Application";
    public string BasePath { get; set; } = null!;
    public bool IsTestMode { get; set; } = false;
    public DateTime TestModeStartDateTimeUtc { get; set; } = DateTime.UtcNow;
    public static TimeSpan TimeSinceStartup => DateTime.UtcNow - ApplicationStartupInfo.StartupTimeUtc;
    public DateTime CurrentTimeUtc => IsTestMode ? TestModeStartDateTimeUtc.Add(TimeSinceStartup) : DateTime.UtcNow;
    public static int GameCheckFrequencyInSeconds => 10;
    public string? InternalApiKey { get; set; }
    public string? InternalApiBasePath { get; set; }

    public bool HasValidBasePath() =>
        Uri.TryCreate(BasePath, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    // BasePath is configured without a trailing slash (e.g. https://draftpuck.com), so join explicitly.
    public string GetAbsoluteUrl(string relativePath) => $"{BasePath.TrimEnd('/')}/{relativePath.TrimStart('/')}";
}
