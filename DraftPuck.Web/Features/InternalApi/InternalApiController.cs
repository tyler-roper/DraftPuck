using DraftPuck.Shared.Discord;
using DraftPuck.Web.Filters;

namespace DraftPuck.Web.Features.InternalApi;

[InternalApiAuth]
[Route("api/internal")]
[ApiController]
public class InternalApiController(IMediator mediator) : ControllerBase
{
    [HttpPost("discord-server-joined")]
    public async Task<IActionResult> DiscordServerJoined(DiscordServerJoinedRequestDto request)
    {
        await mediator.Publish(new DiscordServerJoinedNotification(request.DiscordUserId));
        return Ok();
    }
}
