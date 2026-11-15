namespace DraftPuck.Application.Common.Options;

/// <summary>
/// The Lucky Penny license: one bundle key for MediatR and AutoMapper. Without it both work but log a warning; using
/// them in production requires one. Set with DRAFTPUCK_LUCKYPENNY__LICENSEKEY.
/// </summary>
public static class LuckyPennyOptions
{
    /// <summary>The configuration key holding the license key.</summary>
    public const string LicenseKey = "LuckyPenny:LicenseKey";
}
