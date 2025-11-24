namespace DraftPuck.Application.Features.System;

public class SystemSettingsResponse
{
    public bool IsTestMode { get; set; } = false;
    public DateTime TestModeStartDateTimeUtc { get; set; }
    public DateTime StartupTimeUtc { get; set; }
    public string GitSha { get; set; } = null!;
}