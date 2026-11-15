using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.System;

public class GetSystemSettingsQueryHandler(
    IOptions<ApplicationOptions> appConfig,
    IOptions<SystemOptions> systemConfig,
    IDbContext dbContext) : IRequestHandler<GetSystemSettingsQuery, SystemSettingsResponse>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;
    private readonly SystemOptions _systemConfig = systemConfig.Value;

    public async Task<SystemSettingsResponse> Handle(GetSystemSettingsQuery request, CancellationToken ct)
    {
        return new SystemSettingsResponse
        {
            IsTestMode = _appConfig.IsTestMode,
            TestModeStartDateTimeUtc = _appConfig.TestModeStartDateTimeUtc,
            StartupTimeUtc = ApplicationStartupInfo.StartupTimeUtc,
            GitSha = _systemConfig.GitSha,
            EventLogo = await GetCurrentEventLogoAsync(ct)
        };
    }

    /// <summary>
    /// The event on now (by the app's clock, so test mode shows its simulated date's event); the latest-starting one if
    /// several are. Not cached: settings are fetched once per visit and this is one indexed query on a tiny table, so the
    /// database stays the only source of truth, whatever the number of replicas.
    /// </summary>
    private Task<EventLogoDto?> GetCurrentEventLogoAsync(CancellationToken ct)
    {
        var now = _appConfig.CurrentTimeUtc;
        return dbContext.EventLogos
            .AsNoTracking()
            .Where(e => e.StartsUtc <= now && now < e.EndsUtc)
            .OrderByDescending(e => e.StartsUtc)
            .Select(e => new EventLogoDto { Name = e.Name, LogoUrl = e.LogoUrl, WideLogoUrl = e.WideLogoUrl })
            .FirstOrDefaultAsync(ct);
    }
}
