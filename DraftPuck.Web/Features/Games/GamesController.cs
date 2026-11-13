using DraftPuck.Application.Features.Games;
using Microsoft.AspNetCore.Authorization;

namespace DraftPuck.Web.Features.Games;

[AllowAnonymous]
public class GamesController(IMediator mediator) : BaseController()
{
    // Lobby pages fetch every game on (re)connect; after that each game is pushed over SignalR ("GameUpdated").
    [HttpGet]
    public async Task<ActionResult<List<GameDto>>> GetAllGames()
    {
        var query = new GetAllGamesQuery();
        var games = await mediator.Send(query);
        return Ok(games);
    }

    [HttpGet("summaries")]
    public async Task<ActionResult<List<GameSummaryDto>>> GetAllGameSummaries()
    {
        var query = new GetAllGameSummariesQuery();
        var summaries = await mediator.Send(query);
        return Ok(summaries);
    }
}