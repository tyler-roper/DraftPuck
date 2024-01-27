namespace DraftPuck.Core.Services.Interfaces;

public interface IGameService
{
    public Task CheckGamesAsync();
    public Game GetGameById(int id);
    public List<Game> GetAllGames();
    public List<GameSummary> GetAllGameSummaries();
}
