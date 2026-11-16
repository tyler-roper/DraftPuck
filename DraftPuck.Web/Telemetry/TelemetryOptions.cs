namespace DraftPuck.Web.Telemetry;

public class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Application Insights connection string. Falls back to APPLICATIONINSIGHTS_CONNECTION_STRING, the standard name
    /// all three apps use in Azure. Without either (local development), nothing is exported.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>Share of requests traced, 0 to 1. Logs inside an unsampled request are dropped with it, so keep this at 1 unless traffic makes it costly.</summary>
    public float SampleRate { get; set; } = 1f;
}
