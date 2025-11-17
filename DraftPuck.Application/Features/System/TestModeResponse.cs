namespace DraftPuck.Application.Features.System;

public class TestModeResponse
{
    public bool IsTestMode { get; set; } = false;
    public DateTime TestModeStartDateTimeUtc { get; set; }
    public DateTime StartupTimeUtc { get; set; }
}