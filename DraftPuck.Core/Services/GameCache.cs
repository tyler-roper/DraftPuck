using DraftPuck.Shared.Models;
using System.Collections.Concurrent;

namespace DraftPuck.Core.Services;

public class GameCache : IGameCache
{
    private readonly ConcurrentDictionary<int, Game> _games = new();


    public Game? GetGameById(int id)
    {
        return _games.TryGetValue(id, out var game) ? game : null;
    }

    public List<Game> GetAllGames()
    {
        return _games.Values.ToList();
    }

    public void RemoveGame(int id)
    {
        _games.TryRemove(id, out _);
    }

    public void RemoveGame(Game game)
    {
        _games.TryRemove(game.Id, out _);
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
