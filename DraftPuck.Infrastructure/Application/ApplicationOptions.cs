namespace DraftPuck.Infrastructure.Application;

public class ApplicationOptions
{
    public const string SectionName = "Application";
    public string BasePath { get; set; } = null!;
    public bool IsTestMode { get; set; } = false;
    public DateTime TestModeStartDateTimeUtc { get; set; } = DateTime.UtcNow;
    public static TimeSpan TimeSinceStartup => DateTime.UtcNow - ApplicationStartupInfo.StartupTimeUtc;
    public DateTime CurrentTimeUtc => IsTestMode ? TestModeStartDateTimeUtc.Add(TimeSinceStartup) : DateTime.UtcNow;
}
