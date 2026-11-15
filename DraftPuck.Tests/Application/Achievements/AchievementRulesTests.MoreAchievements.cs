using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Achievements;
using DraftPuck.Application.Features.Achievements.Rules;
using DraftPuck.Domain.Achievements;

namespace DraftPuck.Tests.Application.Achievements;

/// <summary>The second batch of achievements: goal moments, lobby results, drink counts and showing up.</summary>
public partial class AchievementRulesTests
{
    private const int Scorer = 100; // TestGames.HomePlayer(0)

    [Fact]
    public async Task EveryAchievementWithARuleHasABannerWhoseImageExists()
    {
        // Verified Contributor is awarded by hand, so it has no rule.
        var ruleIds = typeof(BaseAchievementRule).Assembly.GetTypes()
            .Where(t => typeof(IAchievementRule).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => ((IAchievementRule)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(t)).UniqueIdentifier)
            .ToList();
        var bannersFolder = Path.Combine(FindRepoRoot(), "DraftPuck.Web", "ClientApp", "public");

        await using var db = fixture.CreateContext();
        var banners = await db.Banners.Where(b => b.AchievementId != null)
            .Select(b => new { Achievement = b.Achievement!.UniqueIdentifier, b.ImagePath })
            .ToListAsync();

        Assert.All(ruleIds, id => Assert.Contains(banners, b => b.Achievement == id));
        Assert.All(banners, b => Assert.True(File.Exists(Path.Combine(bannersFolder, b.ImagePath.TrimStart('/'))), $"Missing image {b.ImagePath}"));

        static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DraftPuck.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }

    private static PlayDto Goal(int id, int home, int away, int scorer = Scorer, int teamId = TestGames.HomeTeamId,
        PeriodType periodType = PeriodType.Regulation, string timeRemaining = "10:00", string? situation = "1551")
    {
        var goal = TestGames.Goal(id, periodType == PeriodType.Overtime ? 4 : 1, "10:00", scorer, teamId);
        goal.PeriodType = periodType;
        goal.TimeRemainingInPeriod = timeRemaining;
        goal.HomeScore = home;
        goal.AwayScore = away;
        goal.SituationCode = situation;
        return goal;
    }

    /// <summary>A cached game with these goals, and a user whose pick of the scorer earned a drink for <paramref name="scoringPlay"/>.</summary>
    private async Task<Guid> ScoredAsync(PlayDto[] goals, int scoringPlay, int scorer = Scorer, DateTime? pickMade = null,
        GameState state = GameState.Live, Action<GameDto>? configure = null)
    {
        var gameId = Seed.NewGameId();
        var game = TestGames.Create(gameId, DateTime.UtcNow.AddHours(-2), state);
        game.Plays = [.. goals];
        configure?.Invoke(game);
        await _cache.AddGameAsync(game);

        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (_, host) = s.Lobby([gameId]);
            user = host.UserId;
            var pick = s.Pick(host, gameId, TestGames.HomeTeamId, scorer, pickMade ?? game.DateTime.AddMinutes(-10));
            var drink = Drink(db, pick);
            drink.EventId = scoringPlay;
        });
        return user;
    }

    // --- Goal moments ---

    [Theory]
    [InlineData(PeriodType.Overtime, true)]
    [InlineData(PeriodType.Regulation, false)]
    public async Task SuddenDeathIsAnOvertimeGoal(PeriodType period, bool expected)
    {
        var user = await ScoredAsync([Goal(1, 1, 0, periodType: period)], 1);

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.DrinkAwarded)).Contains("sudden_death"));
    }

    [Theory]
    [InlineData("00:07", true)]
    [InlineData("00:15", false)]
    public async Task BuzzerBeaterIsAGoalWithUnderTenSecondsLeft(string remaining, bool expected)
    {
        var user = await ScoredAsync([Goal(1, 1, 0, timeRemaining: remaining)], 1);

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.DrinkAwarded)).Contains("buzzer_beater"));
    }

    [Fact]
    public async Task FirstBloodIsTheGamesFirstGoal()
    {
        var first = await ScoredAsync([Goal(1, 1, 0)], 1);
        var second = await ScoredAsync([Goal(1, 0, 1, scorer: 200, teamId: TestGames.AwayTeamId), Goal(2, 1, 1)], 2);

        Assert.Contains("first_blood", await CheckAsync(first, AchievementTriggerType.DrinkAwarded));
        Assert.DoesNotContain("first_blood", await CheckAsync(second, AchievementTriggerType.DrinkAwarded));
    }

    [Fact]
    public async Task ManAdvantageAndEmptyNetterReadWhoWasOnTheIce()
    {
        // Codes are away goalie, away skaters, home skaters, home goalie; the scorer plays for the home team.
        var powerPlay = await ScoredAsync([Goal(1, 1, 0, situation: "1451")], 1);
        var evenStrength = await ScoredAsync([Goal(1, 1, 0, situation: "1551")], 1);
        var emptyNet = await ScoredAsync([Goal(1, 1, 0, situation: "0651")], 1); // away goalie pulled for an extra skater

        var pp = await CheckAsync(powerPlay, AchievementTriggerType.DrinkAwarded);
        Assert.Contains("man_advantage", pp);
        Assert.DoesNotContain("empty_netter", pp);
        Assert.DoesNotContain("man_advantage", await CheckAsync(evenStrength, AchievementTriggerType.DrinkAwarded));
        var en = await CheckAsync(emptyNet, AchievementTriggerType.DrinkAwarded);
        Assert.Contains("empty_netter", en);
        Assert.DoesNotContain("man_advantage", en);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(-10, false)]
    public async Task LateCallUpIsAPickMadeAfterPuckDropThatScores(int minutesAfterStart, bool expected)
    {
        var start = DateTime.UtcNow.AddHours(-2);
        var user = await ScoredAsync([Goal(1, 1, 0)], 1, pickMade: start.AddMinutes(minutesAfterStart),
            configure: g => g.DateTime = start);

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.DrinkAwarded)).Contains("late_call_up"));
    }

    [Fact]
    public async Task GameWinnerIsTheGoalThatPutTheWinnerAheadForGood()
    {
        // Home wins 3-1: 1-0, 1-1, 2-1 (the winner, by player 101), 3-1.
        PlayDto[] goals = [Goal(1, 1, 0), Goal(2, 1, 1, scorer: 200, teamId: TestGames.AwayTeamId), Goal(3, 2, 1, scorer: 101), Goal(4, 3, 1)];
        var winner = await ScoredAsync(goals, 3, scorer: 101, state: GameState.Final);
        var insurance = await ScoredAsync(goals, 4, state: GameState.Final);

        Assert.Contains("game_winner", await CheckAsync(winner, AchievementTriggerType.GameFinished));
        Assert.DoesNotContain("game_winner", await CheckAsync(insurance, AchievementTriggerType.GameFinished));
    }

    [Fact]
    public async Task AShootoutGameHasNoGameWinner()
    {
        PlayDto[] goals = [Goal(1, 1, 0), Goal(2, 1, 1, scorer: 200, teamId: TestGames.AwayTeamId),
            Goal(3, 2, 1, periodType: PeriodType.Shootout)];
        var user = await ScoredAsync(goals, 1, state: GameState.Final);

        Assert.DoesNotContain("game_winner", await CheckAsync(user, AchievementTriggerType.GameFinished));
    }

    // --- Lobby results ---

    /// <summary>An ended lobby where the user's picks are (team, scored) pairs.</summary>
    private async Task<Guid> EndedLobbyAsync((int TeamId, bool Scored)[] picks, int otherHumans = 0, bool receivedDrink = false)
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()], isActive: false);
            user = host.UserId;
            var others = Enumerable.Range(0, otherHumans).Select(_ => s.Member(lobby)).ToList();
            for (var i = 0; i < picks.Length; i++)
            {
                var pick = s.Pick(host, Seed.NewGameId(), picks[i].TeamId, 100 + i);
                if (picks[i].Scored) Drink(db, pick);
            }
            if (receivedDrink) Drink(db, Pick(s, others[0], 150), recipient: host);
        });
        return user;
    }

    [Fact]
    public async Task HomerIsEveryPickFromOneTeamWithAGoal()
    {
        var homer = await EndedLobbyAsync([(1, true), (1, false), (1, false)]);
        var mixed = await EndedLobbyAsync([(1, true), (2, false)]);
        var noGoal = await EndedLobbyAsync([(1, false), (1, false)]);

        Assert.Contains("homer", await CheckAsync(homer, AchievementTriggerType.LobbyEnded));
        Assert.DoesNotContain("homer", await CheckAsync(mixed, AchievementTriggerType.LobbyEnded));
        Assert.DoesNotContain("homer", await CheckAsync(noGoal, AchievementTriggerType.LobbyEnded));
    }

    [Fact]
    public async Task CleanSheetIsEveryPickScoringAndGooseEggIsNone()
    {
        var clean = await EndedLobbyAsync([(1, true), (2, true), (1, true)]);
        var twoOfThree = await EndedLobbyAsync([(1, true), (2, true), (1, false)]);
        var goose = await EndedLobbyAsync([(1, false), (2, false), (1, false), (2, false)]);
        var onlyThreeBlanks = await EndedLobbyAsync([(1, false), (2, false), (1, false)]);

        Assert.Contains("clean_sheet", await CheckAsync(clean, AchievementTriggerType.LobbyEnded));
        Assert.DoesNotContain("clean_sheet", await CheckAsync(twoOfThree, AchievementTriggerType.LobbyEnded));
        Assert.Contains("goose_egg", await CheckAsync(goose, AchievementTriggerType.LobbyEnded));
        Assert.DoesNotContain("goose_egg", await CheckAsync(onlyThreeBlanks, AchievementTriggerType.LobbyEnded));
    }

    [Fact]
    public async Task DesignatedDriverIsNoDrinksInALobbyOfFiveHumans()
    {
        var sober = await EndedLobbyAsync([(1, false)], otherHumans: 4);
        var hit = await EndedLobbyAsync([(1, false)], otherHumans: 4, receivedDrink: true);
        var small = await EndedLobbyAsync([(1, false)], otherHumans: 3);

        Assert.Contains("designated_driver", await CheckAsync(sober, AchievementTriggerType.LobbyEnded));
        Assert.DoesNotContain("designated_driver", await CheckAsync(hit, AchievementTriggerType.LobbyEnded));
        Assert.DoesNotContain("designated_driver", await CheckAsync(small, AchievementTriggerType.LobbyEnded));
    }

    [Fact]
    public async Task HealthyScratchIsOneOfYourPicksScratched()
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            user = host.UserId;
            db.LobbyEvents.Add(new LobbyEventEntity
            {
                Id = Guid.NewGuid(), LobbyId = lobby.Id, LobbyMemberId = host.Id, LobbyEventType = LobbyEventType.PickScratched,
                Title = "Pick scratched", Text = "x", TimeUtc = DateTime.UtcNow, IsSent = true
            });
        });

        Assert.Contains("healthy_scratch", await CheckAsync(user, AchievementTriggerType.PickScratched));
    }

    // --- Drink counts ---

    [Theory]
    [InlineData(5, 1, "target_practice", true)]  // 5 to one person
    [InlineData(4, 1, "target_practice", false)]
    [InlineData(5, 5, "spread_the_wealth", true)] // one each to 5 people
    [InlineData(4, 4, "spread_the_wealth", false)]
    public async Task TargetPracticeAndSpreadTheWealthCountWhoYouGiveTo(int drinks, int people, string achievement, bool expected)
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            user = host.UserId;
            var recipients = Enumerable.Range(0, people).Select(_ => s.Member(lobby)).ToList();
            var pick = Pick(s, host);
            for (var i = 0; i < drinks; i++) Drink(db, pick, recipient: recipients[i % people]);
        });

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.DrinkAssigned)).Contains(achievement));
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(9, false)]
    public async Task BottomlessCupIsTenDrinksReceivedInOneLobby(int drinks, bool expected)
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            user = host.UserId;
            var pick = Pick(s, s.Member(lobby));
            for (var i = 0; i < drinks; i++) Drink(db, pick, recipient: host);
        });

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.DrinkAssigned)).Contains("bottomless_cup"));
    }

    // --- Showing up ---

    /// <summary>A user who was in a lobby on each of these days ago (one lobby per day, created in the evening).</summary>
    private async Task<Guid> PlayedOnDaysAsync(params int[] daysAgo)
    {
        Guid user = default;
        await SeedAsync((s, _) =>
        {
            var player = s.User();
            user = player.Id;
            foreach (var day in daysAgo)
            {
                var (lobby, _) = s.Lobby([Seed.NewGameId()]);
                lobby.Created = DateTime.UtcNow.Date.AddDays(-day).AddHours(20);
                s.Member(lobby, player);
            }
        });
        return user;
    }

    [Fact]
    public async Task BackToBackIsThreeNightsInARow()
    {
        var streak = await PlayedOnDaysAsync(5, 4, 3);
        var gap = await PlayedOnDaysAsync(5, 4, 2);

        Assert.Contains("back_to_back", await CheckAsync(streak, AchievementTriggerType.LobbyJoined));
        Assert.DoesNotContain("back_to_back", await CheckAsync(gap, AchievementTriggerType.LobbyJoined));
    }

    [Theory]
    [InlineData(25, true)]
    [InlineData(24, false)]
    public async Task SeasonTicketHolderIsTwentyFiveNights(int nights, bool expected)
    {
        var user = await PlayedOnDaysAsync([.. Enumerable.Range(0, nights).Select(i => i * 2)]); // every other night

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.LobbyJoined)).Contains("season_ticket_holder"));
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(49, false)]
    public async Task ChirpMachineIsFiftyMessagesInOneLobby(int messages, bool expected)
    {
        Guid user = default;
        await SeedAsync((s, db) =>
        {
            var (_, host) = s.Lobby([Seed.NewGameId()]);
            user = host.UserId;
            for (var i = 0; i < messages; i++)
                db.Messages.Add(new MessageEntity { Id = Guid.NewGuid(), LobbyMemberId = host.Id, Message = $"chirp {i}", Sent = DateTime.UtcNow });
        });

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.MessageSent)).Contains("chirp_machine"));
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(9, false)]
    public async Task BotArmyIsHostingTheMaximumBots(int bots, bool expected)
    {
        Guid host = default;
        await SeedAsync((s, _) =>
        {
            var (lobby, h) = s.Lobby([Seed.NewGameId()]);
            host = h.UserId;
            for (var i = 0; i < bots; i++) s.Bot(lobby);
        });

        Assert.Equal(expected, (await CheckAsync(host, AchievementTriggerType.HostedLobbyJoined)).Contains("bot_army"));
    }

    [Theory]
    [InlineData(GameType.Playoffs, true)]
    [InlineData(GameType.RegularSeason, false)]
    public async Task PlayoffBeardIsALobbyWithAPlayoffGame(GameType type, bool expected)
    {
        var gameId = Seed.NewGameId();
        var game = TestGames.Create(gameId, DateTime.UtcNow.AddHours(3));
        game.GameType = type;
        await _cache.AddGameAsync(game);
        Guid user = default;
        await SeedAsync((s, _) => user = s.Lobby([gameId]).Host.UserId);

        Assert.Equal(expected, (await CheckAsync(user, AchievementTriggerType.LobbyCreated)).Contains("playoff_beard"));
    }
}
