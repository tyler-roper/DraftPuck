using DraftPuck.Application.Features.Games;

namespace DraftPuck.Web.Features.Games;

public class GamesController(IMediator mediator) : BaseController()
{
    [HttpGet("{gameId}")]
    public async Task<ActionResult<GameDto>> GetGameById(int gameId)
    {
        var query = new GetGameByIdQuery { GameId = gameId };
        var gameDto = await mediator.Send(query);
        return Ok(gameDto);
    }

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