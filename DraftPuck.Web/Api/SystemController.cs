using DraftPuck.Infrastructure.Application;
using Microsoft.Extensions.Options;

namespace DraftPuck.Web.Api;

public class SystemController : DraftPuckApiControllerBase
{
    private readonly IErrorService _errorService;
    private readonly ApplicationOptions _appConfig;

    public SystemController(IErrorService errorService, IOptions<ApplicationOptions> appConfig)
    {
        _errorService = errorService;
        _appConfig = appConfig.Value;
    }

    [HttpPost("errors")]
    public async Task<IActionResult> ReportError(ErrorRequest request)
    {
        await _errorService.Log(request);
        return NoContent();
    }

    [HttpGet("settings")]
    public IActionResult GetTestStartTime()
    {
        return Ok(new TestModeResponse()
        {
            IsTestMode = _appConfig.IsTestMode,
            TestModeStartDateTimeUtc = _appConfig.TestModeStartDateTimeUtc,
            StartupTimeUtc = ApplicationStartupInfo.StartupTimeUtc
        });
    }

    public class TestModeResponse
    {
        public bool IsTestMode { get; set; } = false;
        public DateTime TestModeStartDateTimeUtc { get; set; }
        public DateTime StartupTimeUtc { get; set; }
    }
}