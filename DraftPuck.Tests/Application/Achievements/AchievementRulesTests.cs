using DraftPuck.Application.Features.Achievements;
using DraftPuck.Application.Features.Achievements.Rules;
using DraftPuck.Application.Features.Games;
using DraftPuck.Shared.Achievements;
using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Tests.Application.Achievements;

/// <summary>
/// Each achievement rule against its description, through AchievementAwardService with every rule the app registers,
/// against the real database.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AchievementRulesTests(DatabaseFixture fixture)
{
    private readonly InMemoryGameCache _cache = new();

    private AchievementAwardService AwardService(DraftPuckContext db)
    {
        var services = new ServiceCollection().AddLogging().AddSingleton<IGameCache>(_cache).BuildServiceProvider();
        var rules = typeof(BaseAchievementRule).Assembly.GetTypes()
            .Where(t => typeof(IAchievementRule).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (IAchievementRule)ActivatorUtilities.CreateInstance(services, t));
        return new AchievementAwardService(db, rules, NullLogger<AchievementAwardService>.Instance, new UserRepository(db));
    }

    /// <summary>Runs the check for one trigger and returns the identifiers it awarded.</summary>
    private async Task<string[]> CheckAsync(Guid userId, AchievementTriggerType trigger)
    {
        await using var db = fixture.CreateContext();
        var awarded = await AwardService(db).CheckAndAwardAllAsync(userId, trigger, default);
        return [.. awarded.Select(a => a.UniqueIdentifier)];
    }

    private async Task SeedAsync(Action<Seed, DraftPuckContext> seed)
    {
        await using var db = fixture.CreateContext();
        seed(new Seed(db), db);
        await db.SaveChangesAsync();
    }

    private static DrinkEntity Drink(DraftPuckContext db, LobbyMemberPickEntity pick, LobbyMemberEntity? recipient = null,
        DateTime? created = null, DateTime? assigned = null)
    {
        var drink = new DrinkEntity
        {
            Id = Guid.NewGuid(),
            LobbyMemberPickId = pick.Id,
            RecipientLobbyMemberId = recipient?.Id,
            EventId = Random.Shared.Next(1, int.MaxValue),
            Created = created ?? DateTime.UtcNow,
            Assigned = recipient == null ? null : assigned ?? DateTime.UtcNow
        };
        db.Drinks.Add(drink);
        return drink;
    }

    private static LobbyMemberPickEntity Pick(Seed seed, LobbyMemberEntity member, int playerId = 100, int? gameId = null, DateTime? created = null)
        => seed.Pick(member, gameId ?? Seed.NewGameId(), TestGames.HomeTeamId, playerId, created ?? DateTime.UtcNow.AddHours(-1));

    // --- Lobbies ---

    [Fact]
    public async Task RookieHostIsForCreatingALobby()
    {
        Guid host = default;
        await SeedAsync((s, _) => host = s.Lobby([Seed.NewGameId()]).Host.UserId);

        Assert.Contains("rookie_host", await CheckAsync(host, AchievementTriggerType.LobbyCreated));
    }

    [Fact]
    public async Task PartyCrasherIsForJoiningSomeoneElsesLobbyNotHostingYourOwn()
    {
        Guid host = default, joiner = default;
        await SeedAsync((s, _) =>
        {
            var (lobby, h) = s.Lobby([Seed.NewGameId()]);
            host = h.UserId;
            joiner = s.Member(lobby).UserId;
        });

        Assert.DoesNotContain("party_crasher", await CheckAsync(host, AchievementTriggerType.LobbyJoined));
        Assert.Contains("party_crasher", await CheckAsync(joiner, AchievementTriggerType.LobbyJoined));
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(7, false)]
    public async Task PartyStarterGoesToTheHostOfALobbyWithEightHumans(int humans, bool expected)
    {
        Guid host = default;
        await SeedAsync((s, _) =>
        {
            var (lobby, h) = s.Lobby([Seed.NewGameId()]);
            host = h.UserId;
            for (var i = 1; i < humans; i++) s.Member(lobby);
            s.Bot(lobby); s.Bot(lobby); // bots don't count
        });

        Assert.Equal(expected, (await CheckAsync(host, AchievementTriggerType.HostedLobbyJoined)).Contains("party_starter"));
    }

    [Fact]
    public async Task EarlyRiserComparesWithTheLobbysOwnFirstGame()
    {
        var gameStart = DateTime.UtcNow.AddHours(5);
        var gameId = Seed.NewGameId();
        await _cache.AddGameAsync(TestGames.Create(gameId, gameStart));
        Guid early = default, late = default;
        await SeedAsync((s, _) =>
        {
            var (lobby, host) = s.Lobby([gameId]);
            host.Joined = gameStart.AddHours(-4);
            early = host.UserId;
            var lateMember = s.Member(lobby);
            lateMember.Joined = gameStart.AddHours(-1);
            late = lateMember.UserId;

            // The late joiner was in an older lobby, still active, joined long ago: that doesn't make this join early.
            var (old, _) = s.Lobby([Seed.NewGameId()]);
            var oldMembership = s.Member(old);
            oldMembership.UserId = late;
            oldMembership.Joined = DateTime.UtcNow.AddDays(-2);
        });

        Assert.Contains("early_riser", await CheckAsync(early, AchievementTriggerType.LobbyCreated));
        Assert.DoesNotContain("early_riser", await CheckAsync(late, AchievementTriggerType.LobbyJoined));
    }

    [Fact]
    public async Task EarlyRiserIgnoresOtherGamesInTheCache()
    {
        // An unrelated game starting soon must not decide it: this lobby's game is 6 hours out.
        await _cache.AddGameAsync(TestGames.Create(Seed.NewGameId(), DateTime.UtcNow.AddHours(1)));
        var gameId = Seed.NewGameId();
        await _cache.AddGameAsync(TestGames.Create(gameId, DateTime.UtcNow.AddHours(6)));
        Guid host = default;
        await SeedAsync((s, _) =>
        {
            var (_, h) = s.Lobby([gameId]);
            h.Joined = DateTime.UtcNow;
            host = h.UserId;
        });

        Assert.Contains("early_riser", await CheckAsync(host, AchievementTriggerType.LobbyCreated));
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(2, false)]
    public async Task IncognitoModeIsThreeNameChangesInOneLobby(int changes, bool expected)
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            user = host.UserId;
            for (var i = 0; i < changes; i++)
                db.LobbyEvents.Add(new LobbyEventEntity
                {
                    Id = Guid.NewGuid(), LobbyId = lobby.Id, LobbyMemberId = host.Id, LobbyEventType = LobbyEventType.UserNameChanged,
                    Title = "Name Change", Text = "x", TimeUtc = DateTime.UtcNow, IsSent = true
                });
        });

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.NameChanged)).Contains("incognito_mode"));
    }

    // --- Drinks given and received ---

    [Fact]
    public async Task FirstRounderAndInitiatedAreTheFirstDrinkGivenAndReceived()
    {
        Guid giver = default, receiver = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            var other = s.Member(lobby);
            (giver, receiver) = (host.UserId, other.UserId);
            Drink(db, Pick(s, host), recipient: other);
        });

        Assert.Contains("first_rounder", await CheckAsync(giver, AchievementTriggerType.DrinkAssigned));
        var received = await CheckAsync(receiver, AchievementTriggerType.DrinkAssigned);
        Assert.Contains("initiated", received);
        Assert.DoesNotContain("first_rounder", received);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(4, false)]
    public async Task BartenderAndFiveHoleAreFiveDrinksInOneLobby(int drinks, bool expected)
    {
        Guid giver = default, receiver = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            var other = s.Member(lobby);
            (giver, receiver) = (host.UserId, other.UserId);
            var pick = Pick(s, host);
            for (var i = 0; i < drinks; i++) Drink(db, pick, recipient: other);
        });

        Assert.Equal(expected, (await CheckAsync(giver, AchievementTriggerType.DrinkAssigned)).Contains("the_bartender"));
        Assert.Equal(expected, (await CheckAsync(receiver, AchievementTriggerType.DrinkAssigned)).Contains("five_hole"));
    }

    [Fact]
    public async Task PingedAndTerminatedAreDrinksFromBots()
    {
        Guid oneDrink = default, fiveDrinks = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            var bot = s.Bot(lobby);
            var other = s.Member(lobby);
            (oneDrink, fiveDrinks) = (host.UserId, other.UserId);
            var botPick = Pick(s, bot);
            Drink(db, botPick, recipient: host);
            for (var i = 0; i < 5; i++) Drink(db, botPick, recipient: other);
        });

        var one = await CheckAsync(oneDrink, AchievementTriggerType.DrinkAssigned);
        Assert.Contains("pinged", one);
        Assert.DoesNotContain("terminated", one);
        Assert.Contains("terminated", await CheckAsync(fiveDrinks, AchievementTriggerType.DrinkAssigned));
    }

    [Fact]
    public async Task PuppetMasterIsTheHostWhenBotsHandOutThreeDrinksToOthers()
    {
        Guid host = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, h) = s.Lobby([Seed.NewGameId()]);
            host = h.UserId;
            var bot = s.Bot(lobby);
            var other = s.Member(lobby);
            var botPick = Pick(s, bot);
            for (var i = 0; i < 3; i++) Drink(db, botPick, recipient: other); // none of them to the host
        });

        Assert.Contains("puppet_master", await CheckAsync(host, AchievementTriggerType.DrinkAssigned));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(9, false)]
    public async Task EightBitIsGivingAndReceivingWithTheSamePlayerWithinEightMinutes(int minutesApart, bool expected)
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            var friend = s.Member(lobby);
            user = host.UserId;
            var start = DateTime.UtcNow.AddHours(-1);
            Drink(db, Pick(s, host), recipient: friend, assigned: start);
            Drink(db, Pick(s, friend, 101), recipient: host, assigned: start.AddMinutes(minutesApart));
        });

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.DrinkAssigned)).Contains("8_bit"));
    }

    [Fact]
    public async Task EightBitNeedsTheSamePlayerBothWays()
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            var a = s.Member(lobby);
            var b = s.Member(lobby);
            user = host.UserId;
            Drink(db, Pick(s, host), recipient: a);
            Drink(db, Pick(s, b, 101), recipient: host);
        });

        Assert.DoesNotContain("8_bit", await CheckAsync(user, AchievementTriggerType.DrinkAssigned));
    }

    // --- Goals ---

    [Theory]
    [InlineData(30, true)]
    [InlineData(90, false)]
    public async Task LightTheLampIsAGoalWithinAMinuteOfPicking(int secondsAfterPick, bool expected)
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (_, host) = s.Lobby([Seed.NewGameId()]);
            user = host.UserId;
            var picked = DateTime.UtcNow.AddHours(-1);
            Drink(db, Pick(s, host, created: picked), created: picked.AddSeconds(secondsAfterPick));
        });

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.DrinkAwarded)).Contains("light_the_lamp"));
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(2, false)]
    public async Task MadHatterIsAHatTrickByOnePick(int goals, bool expected)
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (_, host) = s.Lobby([Seed.NewGameId()]);
            user = host.UserId;
            var pick = Pick(s, host);
            for (var i = 0; i < goals; i++) Drink(db, pick);
        });

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.DrinkAwarded)).Contains("mad_hatter"));
    }

    [Fact]
    public async Task HotHandIsTwoDifferentPlayersScoringWithinAMinute()
    {
        Guid twoPlayers = default, onePlayerTwice = default;
        var at = DateTime.UtcNow.AddHours(-1);
        await SeedAsync((s, db) =>
        {
            var (_, a) = s.Lobby([Seed.NewGameId()]);
            twoPlayers = a.UserId;
            Drink(db, Pick(s, a, 100), created: at);
            Drink(db, Pick(s, a, 101), created: at.AddSeconds(40));

            var (_, b) = s.Lobby([Seed.NewGameId()]);
            onePlayerTwice = b.UserId;
            var pick = Pick(s, b, 100);
            Drink(db, pick, created: at);
            Drink(db, pick, created: at.AddSeconds(40));
        });

        Assert.Contains("hot_hand", await CheckAsync(twoPlayers, AchievementTriggerType.DrinkAwarded));
        Assert.DoesNotContain("hot_hand", await CheckAsync(onePlayerTwice, AchievementTriggerType.DrinkAwarded));
    }

    [Fact]
    public async Task TheGamblerCountsEveryoneTiedAtTheBottomAndIgnoresGoalies()
    {
        // Skaters with 5, 4, 3, 2, 1, 0, 0, 0, 0 goals, plus two goalies. The bottom third of the 9 skaters is any
        // skater at or below the 3rd-lowest total (0), so all four 0-goal skaters count, even the last one listed.
        var gameId = Seed.NewGameId();
        var game = TestGames.Create(gameId, DateTime.UtcNow.AddHours(-1), GameState.Live, playersPerTeam: 9);
        int[] goals = [5, 4, 3, 2, 1, 0, 0, 0, 0];
        for (var i = 0; i < goals.Length; i++) game.HomeTeam.Roster[i].Goals = goals[i];
        // Goalies (0 goals) listed before the 0-goal skaters, which used to push the last skater out of the bottom third.
        game.HomeTeam.Roster.Insert(5, new PlayerDto { Id = 190, TeamId = TestGames.HomeTeamId, Position = "G", FirstName = "G", LastName = "1", Headshot = "" });
        game.HomeTeam.Roster.Insert(5, new PlayerDto { Id = 191, TeamId = TestGames.HomeTeamId, Position = "G", FirstName = "G", LastName = "2", Headshot = "" });
        await _cache.AddGameAsync(game);

        Guid lastZero = default, oneGoal = default;
        await SeedAsync((s, db) =>
        {
            var (_, a) = s.Lobby([gameId]);
            lastZero = a.UserId;
            Drink(db, Pick(s, a, TestGames.HomePlayer(8), gameId));

            var (_, b) = s.Lobby([gameId]);
            oneGoal = b.UserId;
            Drink(db, Pick(s, b, TestGames.HomePlayer(4), gameId));
        });

        Assert.Contains("the_gambler", await CheckAsync(lastZero, AchievementTriggerType.DrinkAwarded));
        Assert.DoesNotContain("the_gambler", await CheckAsync(oneGoal, AchievementTriggerType.DrinkAwarded));
    }

    // --- The Cup ---

    private async Task<(Guid Top, Guid Second, LobbyEntity Lobby)> CupLobbyAsync(int humans, int topDrinks, int secondDrinks, int botDrinks, bool isActive = false)
    {
        Guid top = default, second = default;
        LobbyEntity lobby = null!;
        await SeedAsync((s, db) =>
        {
            (lobby, var host) = s.Lobby([Seed.NewGameId()], isActive: isActive);
            var others = Enumerable.Range(1, humans - 1).Select(_ => s.Member(lobby)).ToList();
            var bot = s.Bot(lobby);
            (top, second) = (host.UserId, others[0].UserId);
            var topPick = Pick(s, host);
            for (var i = 0; i < topDrinks; i++) Drink(db, topPick, recipient: others[0]);
            var secondPick = Pick(s, others[0], 101);
            for (var i = 0; i < secondDrinks; i++) Drink(db, secondPick, recipient: host);
            var botPick = Pick(s, bot, 102);
            for (var i = 0; i < botDrinks; i++) Drink(db, botPick, recipient: host);
        });
        return (top, second, lobby);
    }

    [Fact]
    public async Task TheCupGoesToWhoeverGaveOutTheMostOnceTheLobbyEnds()
    {
        var (top, second, _) = await CupLobbyAsync(humans: 5, topDrinks: 4, secondDrinks: 2, botDrinks: 3);

        Assert.Contains("the_cup", await CheckAsync(top, AchievementTriggerType.LobbyEnded));
        Assert.DoesNotContain("the_cup", await CheckAsync(second, AchievementTriggerType.LobbyEnded));
    }

    [Fact]
    public async Task TheCupCountsBots()
    {
        var (top, _, _) = await CupLobbyAsync(humans: 5, topDrinks: 4, secondDrinks: 2, botDrinks: 5);

        Assert.DoesNotContain("the_cup", await CheckAsync(top, AchievementTriggerType.LobbyEnded));
    }

    [Fact]
    public async Task TheCupIsSharedByATie()
    {
        var (top, second, _) = await CupLobbyAsync(humans: 5, topDrinks: 3, secondDrinks: 3, botDrinks: 1);

        Assert.Contains("the_cup", await CheckAsync(top, AchievementTriggerType.LobbyEnded));
        Assert.Contains("the_cup", await CheckAsync(second, AchievementTriggerType.LobbyEnded));
    }

    [Fact]
    public async Task TheCupNeedsFiveHumansAndAnEndedLobby()
    {
        var (small, _, _) = await CupLobbyAsync(humans: 4, topDrinks: 4, secondDrinks: 0, botDrinks: 0);
        var (stillGoing, _, _) = await CupLobbyAsync(humans: 5, topDrinks: 4, secondDrinks: 0, botDrinks: 0, isActive: true);

        Assert.DoesNotContain("the_cup", await CheckAsync(small, AchievementTriggerType.LobbyEnded));
        Assert.DoesNotContain("the_cup", await CheckAsync(stillGoing, AchievementTriggerType.LobbyEnded));
    }

    // --- Awarding ---

    [Fact]
    public async Task AnAchievementIsAwardedOnceEvenWhenChecksRace()
    {
        Guid host = default;
        await SeedAsync((s, _) => host = s.Lobby([Seed.NewGameId()]).Host.UserId);

        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => CheckAsync(host, AchievementTriggerType.LobbyCreated)));

        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.UserAchievements.CountAsync(ua => ua.UserId == host && ua.Achievement.UniqueIdentifier == "rookie_host"));
        Assert.Equal(1, await db.UserBanners.CountAsync(ub => ub.UserId == host && ub.Banner.UniqueIdentifier == "rookie_host"));
    }

    [Fact]
    public async Task BotsDontEarnAchievements()
    {
        Guid bot = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            var botMember = s.Bot(lobby);
            bot = botMember.UserId;
            Drink(db, Pick(s, botMember), recipient: host);
        });

        Assert.Empty(await CheckAsync(bot, AchievementTriggerType.DrinkAssigned));
        Assert.Empty(await CheckAsync(bot, AchievementTriggerType.LobbyJoined));
    }
}
