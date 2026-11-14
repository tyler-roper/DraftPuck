namespace DraftPuck.Application.Features.System;

/// <summary>Fetched once per visit, when the site starts.</summary>
public class SystemSettingsResponse
{
    public bool IsTestMode { get; set; } = false;
    public DateTime TestModeStartDateTimeUtc { get; set; }
    public DateTime StartupTimeUtc { get; set; }
    public string GitSha { get; set; } = null!;

    /// <summary>The event logo to show instead of the usual one, if an event is on.</summary>
    public EventLogoDto? EventLogo { get; set; }
}

public class EventLogoDto
{
    public string Name { get; set; } = null!;
    public string LogoUrl { get; set; } = null!;
    public string WideLogoUrl { get; set; } = null!;
}
