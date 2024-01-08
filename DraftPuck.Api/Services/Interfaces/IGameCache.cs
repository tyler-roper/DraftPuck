namespace DraftPuck.Api.Services.Interfaces
{
    public interface IGameCache
    {
        public Game? GetGameById(int gameId);
        public List<Game> GetAllGames();
        public void RemoveGame(int gameId);
        public void RemoveGame(Game game);
        public void AddGame(Game game);
        public void UpdateGame(Game game);
    }
}
