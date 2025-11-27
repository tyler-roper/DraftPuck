namespace DraftPuck.Web.Telemetry;

public class TelemetryOptions
{
    public const string SectionName = "Telemetry";
    public string? ConnectionString { get; set; }
    public bool EnableTracing { get; set; } = true;
    public double SampleRate { get; set; } = 0.25;
}
