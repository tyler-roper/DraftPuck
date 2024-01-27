namespace DraftPuck.Web.Api;

public class GamesController : DraftPuckApiControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet("{gameId}")]
    public IActionResult GetGameById(int gameId)
    {
        return Json(_gameService.GetGameById(gameId));
    }

    [HttpGet]
    public IActionResult GetAllGames()
    {
        return Json(_gameService.GetAllGames());
    }

    [HttpGet("summaries")]
    public IActionResult GetAllGameSummaries()
    {
        return Json(_gameService.GetAllGameSummaries());
    }
}
