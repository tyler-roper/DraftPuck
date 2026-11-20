using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Tests.Application.Games;

public class GetGameQueryTests
{
    private readonly InMemoryGameCache _cache = new();

    [Fact]
    public async Task ReturnsTheCachedGame()
    {
        var game = TestGames.Create(Seed.NewGameId(), DateTime.UtcNow, GameState.Live);
        await _cache.AddGameAsync(game);

        var found = await new GetGameQueryHandler(_cache).Handle(new GetGameQuery(game.Id), default);

        Assert.Equal(game.Id, found.Id);
    }

    [Fact]
    public async Task AGameThatIsNotCachedIsNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() => new GetGameQueryHandler(_cache).Handle(new GetGameQuery(Seed.NewGameId()), default));
}
