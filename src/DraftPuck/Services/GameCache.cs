using DraftPuck.Models.NhlApi;
using DraftPuck.Services.Interfaces;
using System.Collections.Concurrent;

namespace DraftPuck.Services
{
    public class GameCache : IGameCache
    {
        private readonly ConcurrentDictionary<long, LiveGame> _games = new();

        public LiveGame? GetGameByPk(long gamePk)
        {
            if (_games.TryGetValue(gamePk, out LiveGame? game))
                return game;
            else
                return null;
        }

        public List<LiveGame> GetAllGames()
        {
            return _games.Values.ToList();
        }

        public void RemoveGame(long gamePk)
        {
            _games.TryRemove(gamePk, out LiveGame _);
        }

        public void RemoveGame(LiveGame game)
        {
            _games.TryRemove(game.GamePk, out LiveGame _);
        }

        public void AddGame(LiveGame game)
        {
            _games.TryAdd(game.GamePk, game);
        }

        public void UpdateGame(LiveGame game)
        {
            _games[game.GamePk] = game;
        }
    }
}
