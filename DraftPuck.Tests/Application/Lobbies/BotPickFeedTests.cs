using DraftPuck.Application;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Picks;
using DraftPuck.Shared.Achievements;
using DraftPuck.Shared.Firebase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Lobbies;

/// <summary>
/// What the feed shows for bots' picks, through the real handlers and SQL Server: one item per game when the bots pick
/// (once the game's lineups are out), and a named "replacement" item when a bot re-picks after a scratch.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BotPickFeedTests(DatabaseFixture fixture)
{
    private readonly int _gameId = Seed.NewGameId();

    private ServiceProvider BuildServices() => new ServiceCollection()
        .AddLogging()
        .AddApplication(new ConfigurationBuilder().Build())
        .AddSingleton(TestServices.Mapper())
        .AddSingleton(Options.Create(new ApplicationOptions()))
        .AddSingleton<IGameCache>(new InMemoryGameCache())
        .AddSingleton(Substitute.For<INhlClient>())
        .AddSingleton(Substitute.For<INhlQueueService>())
        .AddSingleton(Substitute.For<IAchievementQueueService>())
        .AddSingleton(Substitute.For<IPushNotificationService>())
        .AddSingleton(Substitute.For<IClientEventService>())
        .AddScoped<IDbContext>(_ => fixture.CreateContext())
        .BuildServiceProvider();

    private async Task InScopeAsync(Func<IMediator, Task> action)
    {
        await using var services = BuildServices();
        await using var scope = services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<IMediator>());
    }

    private Task BotsPickAsync() => InScopeAsync(m => m.Publish(new BotPicksReadyNotification(TestGames.Create(_gameId, DateTime.UtcNow.AddMinutes(45)))));

    private async Task<List<string>> FeedAsync(Guid lobbyId)
    {
        await using var db = fixture.CreateContext();
        return await db.LobbyEvents
            .Where(e => e.LobbyId == lobbyId && (e.LobbyEventType == LobbyEventType.BotPicksMade || e.LobbyEventType == LobbyEventType.PickScratched))
            .OrderBy(e => e.TimeUtc)
            .Select(e => e.LobbyEventType == LobbyEventType.BotPicksMade ? e.Text : "scratched")
            .ToListAsync();
    }

    [Fact]
    public async Task OneItemWhenAllBotsPickForAGameThenANamedReplacementAfterAScratch()
    {
        Guid lobbyId, robotUserId;
        string code, robotName;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([_gameId]);
            seed.Bot(lobby);
            var robot = seed.Bot(lobby);
            await db.SaveChangesAsync();
            (lobbyId, code, robotName, robotUserId) = (lobby.Id, lobby.JoinCode, robot.Name, robot.UserId);
        }

        await BotsPickAsync();
        Assert.Equal(["All bots have made their picks for HOM vs AWY."], await FeedAsync(lobbyId));

        Guid scratchedPickId;
        await using (var db = fixture.CreateContext())
            scratchedPickId = await db.LobbyMemberPicks
                .Where(p => p.LobbyMember.UserId == robotUserId && p.GameId == _gameId && p.IsActive)
                .Select(p => p.Id).FirstAsync();
        await InScopeAsync(m => m.Send(new RemovePickCommand { Code = code, PickId = scratchedPickId, IsSystemRemoval = true }));
        await BotsPickAsync();

        Assert.Equal(
            ["All bots have made their picks for HOM vs AWY.", "scratched", $"<strong>{robotName}</strong> picked a replacement for HOM vs AWY."],
            await FeedAsync(lobbyId));
    }
}
