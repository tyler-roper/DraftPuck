using DraftPuck.Application.Features.Discord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DraftPuck.Web.Features.Discord;

public class DiscordController(IMediator mediator, IOptions<ApplicationOptions> appConfig) : BaseController()
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    [HttpGet("link")]
    public async Task<IActionResult> Link()
    {
        if (!IsSignedIn) throw new UnauthorizedAccessException();
        var discordRedirectUrl = await mediator.Send(new BeginLinkDiscordUserCommand(CurrentUserId));
        return Ok(new { url = discordRedirectUrl });
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect(GetRedirectUrl(false));

        var isSuccess = await mediator.Send(new CompleteLinkDiscordUserCommand(code, state));
        if (!isSuccess)
            return Redirect(GetRedirectUrl(false));

        return Redirect(GetRedirectUrl(true));
    }

    private string GetRedirectUrl(bool isSuccessful)
    {
        var urlBase = $"{_appConfig.BasePath}/account/discord?state=link-";
        return urlBase + (isSuccessful ? "success" : "failed");
    }
}