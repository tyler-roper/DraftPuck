namespace DraftPuck.Web.Api;

public class SystemController : DraftPuckApiControllerBase
{
    private readonly IErrorService _errorService;

    public SystemController(IErrorService errorService)
    {
        _errorService = errorService;
    }

    [HttpPost]
    public async Task<IActionResult> ReportError(ErrorRequest  request)
    {
        await _errorService.Log(request);
        return NoContent();
    }
}