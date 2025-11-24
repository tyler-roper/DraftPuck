using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.System;

public class GetSystemSettingsQueryHandler(IOptions<ApplicationOptions> appConfig, IOptions<SystemOptions> systemConfig) : IRequestHandler<GetSystemSettingsQuery, SystemSettingsResponse>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;
    private readonly SystemOptions _systemConfig = systemConfig.Value;

    public Task<SystemSettingsResponse> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var response = new SystemSettingsResponse
        {
            IsTestMode = _appConfig.IsTestMode,
            TestModeStartDateTimeUtc = _appConfig.TestModeStartDateTimeUtc,
            StartupTimeUtc = ApplicationStartupInfo.StartupTimeUtc,
            GitSha = _systemConfig.GitSha
        };

        return Task.FromResult(response);
    }
}