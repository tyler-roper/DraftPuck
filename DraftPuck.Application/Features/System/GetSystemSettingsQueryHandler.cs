using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.System;

public class GetSystemSettingsQueryHandler(IOptions<ApplicationOptions> appConfig) : IRequestHandler<GetSystemSettingsQuery, TestModeResponse>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public Task<TestModeResponse> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var response = new TestModeResponse
        {
            IsTestMode = _appConfig.IsTestMode,
            TestModeStartDateTimeUtc = _appConfig.TestModeStartDateTimeUtc,
            StartupTimeUtc = ApplicationStartupInfo.StartupTimeUtc
        };

        return Task.FromResult(response);
    }
}