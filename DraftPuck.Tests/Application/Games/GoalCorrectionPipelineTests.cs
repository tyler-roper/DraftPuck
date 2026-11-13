using DraftPuck.Application;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Drinks;
using DraftPuck.Shared.Achievements;
using DraftPuck.Shared.Firebase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Games;

/// <summary>
/// Runs a sequence of NHL polls through the real pipeline (ProcessGameCommand, every MediatR handler, SQL Server)
/// and checks what lobby participants end up with: drinks, and the lobby events shown in the feed.
/// Only the NHL API, the queue, push/achievement queues and SignalR are faked.
///
/// These tests are the agreed behavior for goal corrections:
///   new goal            → drink awarded to each picker of the scorer
///   scorer John → Jane  → "goal changed"; John's pickers' drinks revoked; Jane's pickers awarded
///   goal called back    → "goal removed"; the scorer's pickers' drinks revoked
///   time-only change    → nothing
/// A revoke is "drink invalidated" when the drink was already given to someone, otherwise "drink revoked".
/// They deliberately don't depend on how the handler diffs goals, so they hold across a redesign of it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class GoalCorrectionPipelineTests(DatabaseFixture fixture)
{
    private const int PlayId = 900;
    private static readonly int John = TestGames.HomePlayer(2);
    private static readonly int Jane = TestGames.HomePlayer(3);

    private readonly InMemoryGameCache _cache = new();
    private readonly INhlClient _nhl = Substitute.For<INhlClient>();
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();
    private readonly IClientEventService _clientEvents = Substitute.For<IClientEventService>();
    private readonly int _gameId = Seed.NewGameId();
    private readonly DateTime _start = DateTime.UtcNow;

    // A lobby where the host picked John, a second member picked Jane, and a third member picked nobody in this game.
    // Plus a lobby with the game where nobody has picked, and a lobby for a different game.
    private Guid _lobbyId, _quietLobbyId, _otherGameLobbyId;
    private string _joinCode = null!;
    private Guid _johnPicker, _janePicker, _bystander;
    private Guid _johnPickerUserId;
    private Guid _johnPickId, _janePickId;

    private async Task SeedLobbyAsync()
    {
        await using var db = fixture.CreateContext();
        var seed = new Seed(db);
        var (lobby, host) = seed.Lobby([_gameId], botAutoPicking: false);
        var janeFan = seed.Member(lobby);
        var bystander = seed.Member(lobby);

        _johnPickId = seed.Pick(host, _gameId, TestGames.HomeTeamId, John).Id;
        _janePickId = seed.Pick(janeFan, _gameId, TestGames.HomeTeamId, Jane).Id;
        _quietLobbyId = seed.Lobby([_gameId], botAutoPicking: false).Lobby.Id;
        _otherGameLobbyId = seed.Lobby([Seed.NewGameId()], botAutoPicking: false).Lobby.Id;
        await db.SaveChangesAsync();

        (_lobbyId, _joinCode) = (lobby.Id, lobby.JoinCode);
        (_johnPicker, _janePicker, _bystander) = (host.Id, janeFan.Id, bystander.Id);
        _johnPickerUserId = host.UserId;
    }

    private ServiceProvider BuildServices()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddSingleton(TestServices.Mapper())
            .AddSingleton(Options.Create(new ApplicationOptions()))
            .AddSingleton<IGameCache>(_cache)
            .AddSingleton(_nhl)
            .AddSingleton(Substitute.For<INhlQueueService>())
            .AddSingleton(Substitute.For<IAchievementQueueService>())
            .AddSingleton(_push)
            .AddSingleton(_clientEvents)
            .AddScoped<IDbContext>(_ => fixture.CreateContext());
        return services.BuildServiceProvider();
    }

    /// <summary>A fresh copy of the live game as the NHL API would return it, with the given plays.</summary>
    private GameDto Game(params PlayDto[] plays)
    {
        var game = TestGames.Create(_gameId, _start, GameState.Live);
        game.Plays = [.. plays];
        return game;
    }

    private static PlayDto GoalBy(int scorer, string time = "05:00") => TestGames.Goal(PlayId, 1, time, scorer, TestGames.HomeTeamId);

    /// <summary>One poll, in its own DI scope like a Worker invocation.</summary>
    private async Task PollAsync(GameDto fromNhl)
    {
        _nhl.GetFullGameAsync(_gameId).Returns(fromNhl);
        await using var services = BuildServices();
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new ProcessGameCommand(_gameId, IsInitialPopulation: false));
    }

    /// <summary>The John picker hands their drink for the goal to the bystander.</summary>
    private async Task AssignJohnsDrinkToBystanderAsync()
    {
        var drink = Assert.Single(await DrinksAsync(_johnPickId));
        await using var services = BuildServices();
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new AssignDrinkCommand
        {
            Code = _joinCode,
            DrinkId = drink.Id,
            RecipientLobbyMemberId = _bystander,
            AssignerUserId = _johnPickerUserId
        });
    }

    private async Task<List<DrinkEntity>> DrinksAsync(Guid pickId)
    {
        await using var db = fixture.CreateContext();
        return await db.Drinks.AsNoTracking().Where(d => d.LobbyMemberPickId == pickId).ToListAsync();
    }

    private Task<List<LobbyEventEntity>> EventsAsync(LobbyEventType type) => EventsAsync(_lobbyId, type);

    private async Task<List<LobbyEventEntity>> EventsAsync(Guid lobbyId, LobbyEventType type)
    {
        await using var db = fixture.CreateContext();
        return await db.LobbyEvents.AsNoTracking().Where(e => e.LobbyId == lobbyId && e.LobbyEventType == type).ToListAsync();
    }

    /// <summary>Seeds the lobby, takes a baseline poll with no goals, then a poll where John scores.</summary>
    private async Task JohnScoresAsync()
    {
        await SeedLobbyAsync();
        await PollAsync(Game());
        await PollAsync(Game(GoalBy(John)));
    }

    [Fact]
    public async Task NewGoalAwardsADrinkToEachPickerOfTheScorer()
    {
        await JohnScoresAsync();

        var drink = Assert.Single(await DrinksAsync(_johnPickId));
        Assert.Equal(PlayId, drink.EventId);
        Assert.Null(drink.RecipientLobbyMemberId);
        Assert.Empty(await DrinksAsync(_janePickId));
        Assert.Equal(_johnPicker, Assert.Single(await EventsAsync(LobbyEventType.DrinkAwarded)).LobbyMemberId);
    }

    [Fact]
    public async Task FailedPushToClientsDoesNotStopTheGoalBeingAwarded()
    {
        _clientEvents.SendGameUpdated(Arg.Any<GameDto>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("backplane down"));

        await JohnScoresAsync();

        Assert.Single(await DrinksAsync(_johnPickId));
        await _clientEvents.Received().SendGameUpdated(Arg.Is<GameDto>(g => g.Id == _gameId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollingTheSameGoalAgainChangesNothing()
    {
        await JohnScoresAsync();
        var drink = Assert.Single(await DrinksAsync(_johnPickId));

        await PollAsync(Game(GoalBy(John)));

        Assert.Equal(drink.Id, Assert.Single(await DrinksAsync(_johnPickId)).Id);
        Assert.Single(await EventsAsync(LobbyEventType.DrinkAwarded));
    }

    [Fact]
    public async Task ScorerChangeRevokesUnassignedDrinkAndAwardsTheNewScorersPickers()
    {
        await JohnScoresAsync();

        await PollAsync(Game(GoalBy(Jane)));

        Assert.Empty(await DrinksAsync(_johnPickId));
        Assert.Equal(PlayId, Assert.Single(await DrinksAsync(_janePickId)).EventId);

        Assert.Single(await EventsAsync(LobbyEventType.GoalChanged));
        Assert.Equal(_johnPicker, Assert.Single(await EventsAsync(LobbyEventType.DrinkRevoked)).LobbyMemberId);
        Assert.Empty(await EventsAsync(LobbyEventType.DrinkInvalidated));
        Assert.Contains(await EventsAsync(LobbyEventType.DrinkAwarded), e => e.LobbyMemberId == _janePicker);
    }

    [Fact]
    public async Task GoalChangeIsAnnouncedInEveryLobbyWithTheGame()
    {
        await JohnScoresAsync();

        await PollAsync(Game(GoalBy(Jane)));

        foreach (var lobbyId in new[] { _lobbyId, _quietLobbyId })
        {
            var changed = Assert.Single(await EventsAsync(lobbyId, LobbyEventType.GoalChanged));
            Assert.Equal(Jane, changed.PlayerId);   // {{newScorer}}
            Assert.Equal(John, changed.Player2Id);  // {{oldScorer}}
            Assert.Equal(_gameId, changed.GameId);
            Assert.Equal(PlayId, changed.GameEventId);
        }
        Assert.Empty(await EventsAsync(_otherGameLobbyId, LobbyEventType.GoalChanged));
    }

    [Fact]
    public async Task GoalRemovalIsAnnouncedInEveryLobbyWithTheGame()
    {
        await JohnScoresAsync();

        await PollAsync(Game());

        foreach (var lobbyId in new[] { _lobbyId, _quietLobbyId })
            Assert.Equal(John, Assert.Single(await EventsAsync(lobbyId, LobbyEventType.GoalRemoved)).PlayerId); // {{player}}
        Assert.Empty(await EventsAsync(_otherGameLobbyId, LobbyEventType.GoalRemoved));
    }

    [Fact]
    public async Task ScorerChangeInvalidatesAnAssignedDrinkOnce()
    {
        await JohnScoresAsync();
        await AssignJohnsDrinkToBystanderAsync();

        await PollAsync(Game(GoalBy(Jane)));

        var invalidated = Assert.Single(await EventsAsync(LobbyEventType.DrinkInvalidated));
        Assert.Equal(_johnPicker, invalidated.LobbyMemberId);
        Assert.Equal(_bystander, invalidated.LobbyMember2Id);
        Assert.Single(await EventsAsync(LobbyEventType.GoalChanged));
        Assert.Single(await DrinksAsync(_janePickId));
    }

    [Fact]
    public async Task CalledBackGoalRevokesUnassignedDrink()
    {
        await JohnScoresAsync();

        await PollAsync(Game());

        Assert.Empty(await DrinksAsync(_johnPickId));
        Assert.Single(await EventsAsync(LobbyEventType.GoalRemoved));
        Assert.Equal(_johnPicker, Assert.Single(await EventsAsync(LobbyEventType.DrinkRevoked)).LobbyMemberId);
        Assert.Empty(await EventsAsync(LobbyEventType.DrinkInvalidated));
    }

    [Fact]
    public async Task CalledBackGoalInvalidatesAnAssignedDrinkOnce()
    {
        await JohnScoresAsync();
        await AssignJohnsDrinkToBystanderAsync();

        await PollAsync(Game());

        var invalidated = Assert.Single(await EventsAsync(LobbyEventType.DrinkInvalidated));
        Assert.Equal(_bystander, invalidated.LobbyMember2Id);
        Assert.Single(await EventsAsync(LobbyEventType.GoalRemoved));
    }

    [Fact]
    public async Task TimeOnlyCorrectionChangesNothing()
    {
        await JohnScoresAsync();
        var drink = Assert.Single(await DrinksAsync(_johnPickId));

        await PollAsync(Game(GoalBy(John, time: "05:30")));

        Assert.Equal(drink.Id, Assert.Single(await DrinksAsync(_johnPickId)).Id);
        Assert.Single(await EventsAsync(LobbyEventType.DrinkAwarded));
        Assert.Empty(await EventsAsync(LobbyEventType.DrinkRevoked));
        Assert.Empty(await EventsAsync(LobbyEventType.DrinkInvalidated));
        Assert.Empty(await EventsAsync(LobbyEventType.GoalChanged));
    }

    [Fact]
    public async Task RenumberedGoalChangesNothingForParticipants()
    {
        await JohnScoresAsync();
        var drink = Assert.Single(await DrinksAsync(_johnPickId));

        await PollAsync(Game(TestGames.Goal(PlayId + 1, 1, "05:02", John, TestGames.HomeTeamId)));

        var rekeyed = Assert.Single(await DrinksAsync(_johnPickId));
        Assert.Equal(drink.Id, rekeyed.Id);
        Assert.Equal(PlayId + 1, rekeyed.EventId);
        Assert.Equal(PlayId + 1, Assert.Single(await EventsAsync(LobbyEventType.DrinkAwarded)).GameEventId);
        Assert.Empty(await EventsAsync(LobbyEventType.GoalRemoved));
        Assert.Empty(await EventsAsync(LobbyEventType.DrinkRevoked));
    }

    [Fact]
    public async Task ScorerChangeAfterARenumberStillRevokesTheOldScorersDrink()
    {
        await JohnScoresAsync();
        await PollAsync(Game(TestGames.Goal(PlayId + 1, 1, "05:00", John, TestGames.HomeTeamId)));

        await PollAsync(Game(TestGames.Goal(PlayId + 1, 1, "05:00", Jane, TestGames.HomeTeamId)));

        Assert.Empty(await DrinksAsync(_johnPickId));
        Assert.Single(await DrinksAsync(_janePickId));
        Assert.Single(await EventsAsync(LobbyEventType.DrinkRevoked));
    }

    [Fact]
    public async Task LostCacheAfterARenumberDoesNotAwardTheGoalTwice()
    {
        await JohnScoresAsync();
        await PollAsync(Game(TestGames.Goal(PlayId + 1, 1, "05:00", John, TestGames.HomeTeamId)));
        _cache.Forget(_gameId);

        await PollAsync(Game(TestGames.Goal(PlayId + 1, 1, "05:00", John, TestGames.HomeTeamId)));

        Assert.Single(await DrinksAsync(_johnPickId));
    }

    [Fact]
    public async Task PlayerWhoLeavesTheLineupLosesTheirPicks()
    {
        await SeedLobbyAsync();
        await PollAsync(Game());
        _nhl.GetPlayerAsync(Arg.Any<int>()).Returns(ci => new PlayerDto { Id = ci.Arg<int>(), TeamId = 99 });

        var withoutJohn = Game();
        withoutJohn.PlayerSummaries.RemoveAll(s => s.Id == John);
        await PollAsync(withoutJohn);

        await using var db = fixture.CreateContext();
        Assert.False((await db.LobbyMemberPicks.AsNoTracking().SingleAsync(p => p.Id == _johnPickId)).IsActive);
        Assert.True((await db.LobbyMemberPicks.AsNoTracking().SingleAsync(p => p.Id == _janePickId)).IsActive);
    }

    /// <summary>Polls with John dropped from the NHL lineup, as when he's scratched before puck drop.</summary>
    private async Task ScratchJohnAsync()
    {
        _nhl.GetPlayerAsync(Arg.Any<int>()).Returns(ci => new PlayerDto { Id = ci.Arg<int>(), TeamId = 99 });
        var withoutJohn = Game();
        withoutJohn.PlayerSummaries.RemoveAll(s => s.Id == John);
        await PollAsync(withoutJohn);
    }

    private async Task SetJohnPickerNotificationsAsync(NotificationPreference pickingStarted)
    {
        await using var db = fixture.CreateContext();
        var user = await db.Users.SingleAsync(u => u.Id == _johnPickerUserId);
        user.FcmRegistrationToken = "john-picker-token";
        user.PickingStartedNotificationPreference = pickingStarted;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ScratchIsAnnouncedAsAScratchNotAnUnpick()
    {
        await SeedLobbyAsync();
        await PollAsync(Game());

        await ScratchJohnAsync();

        var scratched = Assert.Single(await EventsAsync(LobbyEventType.PickScratched));
        Assert.Equal(_johnPicker, scratched.LobbyMemberId); // {{name}}
        Assert.Equal(John, scratched.PlayerId);             // {{playerBadge}}
        Assert.Empty(await EventsAsync(LobbyEventType.PickRemoved));
    }

    [Fact]
    public async Task ScratchedPlayerStaysNameableButNotPickable()
    {
        await SeedLobbyAsync();
        await PollAsync(Game());

        await ScratchJohnAsync();

        var cached = (await _cache.GetGameByIdAsync(_gameId))!;
        var former = Assert.Single(cached.FormerPlayers);
        Assert.Equal((John, "Player", "1-2"), (former.Id, former.FirstName, former.LastName));
        Assert.DoesNotContain(cached.HomeTeam.Roster, p => p.Id == John);

        // Still listed on later polls, until he's back in the lineup.
        await PollAsync(WithoutJohn());
        Assert.Single((await _cache.GetGameByIdAsync(_gameId))!.FormerPlayers);
        await PollAsync(Game());
        Assert.Empty((await _cache.GetGameByIdAsync(_gameId))!.FormerPlayers);

        GameDto WithoutJohn()
        {
            var game = Game();
            game.PlayerSummaries.RemoveAll(s => s.Id == John);
            return game;
        }
    }

    [Fact]
    public async Task PickOwnerIsToldTheirPlayerWasScratched()
    {
        await SeedLobbyAsync();
        await SetJohnPickerNotificationsAsync(NotificationPreference.Relevant);
        await PollAsync(Game());

        await ScratchJohnAsync();

        await _push.Received(1).SendLobbyEventNotification(_joinCode, "Pick scratched", "Player 1-2 isn't playing. Pick someone else!",
            "john-picker-token", Arg.Any<Dictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoScratchNotificationWhenPickingNotificationsAreOff()
    {
        await SeedLobbyAsync();
        await SetJohnPickerNotificationsAsync(NotificationPreference.None);
        await PollAsync(Game());

        await ScratchJohnAsync();

        await _push.DidNotReceiveWithAnyArgs().SendLobbyEventNotification(default!, default!, default!, default!);
    }

    [Fact]
    public async Task ChangedLineupRefetchesThatRosterOnTheTeamItPlaysFor()
    {
        await SeedLobbyAsync();
        await PollAsync(Game());
        // The NHL returns each player on their usual club (99), e.g. during an international tournament.
        _nhl.GetPlayerAsync(Arg.Any<int>()).Returns(ci => new PlayerDto { Id = ci.Arg<int>(), TeamId = 99, Goals = ci.Arg<int>() % 7 });

        var newcomer = TestGames.HomePlayer(50);
        var withNewcomer = Game();
        withNewcomer.PlayerSummaries.Add(new PlayerSummaryDto { Id = newcomer, TeamId = TestGames.HomeTeamId, FirstName = "New", LastName = "Player" });
        await PollAsync(withNewcomer);

        var cached = (await _cache.GetGameByIdAsync(_gameId))!;
        Assert.Contains(cached.HomeTeam.Roster, p => p.Id == newcomer);
        Assert.All(cached.HomeTeam.Roster, p => Assert.Equal(TestGames.HomeTeamId, p.TeamId));
        Assert.Equal(cached.HomeTeam.Roster.OrderByDescending(p => p.Goals).Select(p => p.Id), cached.HomeTeam.Roster.Select(p => p.Id));
        // The away lineup didn't change, so it wasn't refetched.
        await _nhl.DidNotReceive().GetPlayerAsync(TestGames.AwayPlayer(0));
    }

    [Fact]
    public async Task GoalScoredWhileTheCachedGameWasLostStillAwardsADrink()
    {
        await SeedLobbyAsync();
        await PollAsync(Game());
        _cache.Forget(_gameId);

        await PollAsync(Game(GoalBy(John)));

        Assert.Single(await DrinksAsync(_johnPickId));
    }
}
