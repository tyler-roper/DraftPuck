using System.Collections.Concurrent;

namespace DraftPuck.Api.Services
{
    public class GameCache : IGameCache
    {
        private readonly ConcurrentDictionary<int, Game> _games = new();


        public Game? GetGameById(int id)
        {
            if (_games.TryGetValue(id, out Game? game))
                return game;
            else
                return null;
        }

        public List<Game> GetAllGames()
        {
            return _games.Values.ToList();
        }

        public void RemoveGame(int id)
        {
            _games.TryRemove(id, out Game _);
        }

        public void RemoveGame(Game game)
        {
            _games.TryRemove(game.Id, out Game _);
        }

        public void AddGame(Game game)
        {
            _games.TryAdd(game.Id, game);
        }

        public void UpdateGame(Game game)
        {
            _games[game.Id] = game;
        }
    }
}
