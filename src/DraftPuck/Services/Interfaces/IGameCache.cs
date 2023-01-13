using DraftPuck.Models.NhlApi;

namespace DraftPuck.Services.Interfaces
{
    public interface IGameCache
    {
        public LiveGame? GetGameByPk(long gamePk);

        public List<LiveGame> GetAllGames();

        public void RemoveGame(long gamePk);

        public void RemoveGame(LiveGame game);

        public void AddGame(LiveGame game);

        public void UpdateGame(LiveGame game);
    }
}
