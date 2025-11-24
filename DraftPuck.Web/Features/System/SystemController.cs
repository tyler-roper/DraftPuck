using DraftPuck.Application.Features.System;
using Microsoft.AspNetCore.Authorization;

namespace DraftPuck.Web.Features.System;

[AllowAnonymous]
public class SystemController(IMediator mediator) : BaseController()
{
    [HttpGet("settings")]
    public async Task<ActionResult<SystemSettingsResponse>> GetSettings()
    {
        var query = new GetSystemSettingsQuery();
        var settings = await mediator.Send(query);
        return Ok(settings);
    }
}