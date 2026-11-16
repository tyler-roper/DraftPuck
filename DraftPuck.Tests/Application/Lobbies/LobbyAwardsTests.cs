using DraftPuck.Application.Features.Lobbies.Recap;

namespace DraftPuck.Tests.Application.Lobbies;

public class LobbyAwardsTests
{
    private static readonly DateTime T0 = new(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc);
    private static readonly RecapMember Sam = new(Guid.NewGuid(), "Sam");
    private static readonly RecapMember Alex = new(Guid.NewGuid(), "Alex");
    private static readonly RecapMember Jo = new(Guid.NewGuid(), "Jo");
    private static readonly RecapMember Bot = new(Guid.NewGuid(), "Bot", IsBot: true);
    private static readonly RecapMember[] Members = [Sam, Alex, Jo, Bot];
    private static int _playerId;

    private static RecapPick Pick(RecapMember member, int drinks, int minute = 0, int? seasonGoals = null, int? startMinute = null) =>
        new(Guid.NewGuid(), member.Id, 1, Interlocked.Increment(ref _playerId), drinks, T0.AddMinutes(minute), seasonGoals,
            startMinute is { } s ? T0.AddMinutes(s) : null);

    private static RecapDrink[] Gave(RecapMember from, RecapMember to, int count, int heldMinutes = 0) =>
        [.. Enumerable.Repeat(new RecapDrink(from.Id, to.Id, T0, T0.AddMinutes(heldMinutes)), count)];

    private static List<LobbyAward> Candidates(RecapPick[]? picks = null, RecapDrink[]? drinks = null,
        Dictionary<Guid, int>? messages = null, RecapAchievement[]? achievements = null) =>
        LobbyAwards.Candidates(new RecapInput(Members, picks ?? [], drinks ?? [], messages ?? [], achievements ?? []));

    private static LobbyAward? Award(List<LobbyAward> awards, string key) => awards.SingleOrDefault(a => a.Key == key);

    // --- Picks ---

    [Fact]
    public void AQuietLobbyGetsNoAwards() => Assert.Empty(Candidates([Pick(Sam, 0)]));

    [Fact]
    public void BestPickIsTheMostDrinksAndTheEarlierPickOnATie()
    {
        var early = Pick(Alex, 3, minute: 1);
        var best = Award(Candidates([Pick(Sam, 3, minute: 5), early, Pick(Jo, 1)]), LobbyAwards.BestPick)!;

        Assert.Equal((early, 3), (best.Pick, best.Value));
    }

    [Fact]
    public void HatTrickIsAnotherPickThatScoredThreeTimes()
    {
        Assert.Null(Award(Candidates([Pick(Sam, 4)]), LobbyAwards.HatTrick)); // that's the best pick

        var second = Pick(Alex, 3, minute: 2);
        Assert.Equal(second, Award(Candidates([Pick(Sam, 4), second]), LobbyAwards.HatTrick)!.Pick);
    }

    [Fact]
    public void SleeperIsTheScorerWithTheFewestSeasonGoals()
    {
        var sleeper = Pick(Jo, 1, seasonGoals: 1);
        var award = Award(Candidates([Pick(Sam, 3, seasonGoals: 20), Pick(Alex, 1, seasonGoals: 8), sleeper, Pick(Jo, 0, seasonGoals: 0)]), LobbyAwards.Sleeper)!;

        Assert.Equal((sleeper, 1), (award.Pick, award.Value));
        Assert.True(award.Strength > Award(Candidates([Pick(Sam, 3), Pick(Alex, 1, seasonGoals: 8)]), LobbyAwards.Sleeper)!.Strength);
    }

    [Fact]
    public void PerfectNightNeedsEveryPickToScore()
    {
        Assert.Null(Award(Candidates([Pick(Sam, 1), Pick(Sam, 0)]), LobbyAwards.PerfectNight));
        Assert.Null(Award(Candidates([Pick(Sam, 1)]), LobbyAwards.PerfectNight));

        Assert.Equal(Sam.Id, Award(Candidates([Pick(Sam, 1), Pick(Sam, 2)]), LobbyAwards.PerfectNight)!.MemberId);
    }

    [Fact]
    public void BuzzerBeaterIsTheLatestScoringPickNearPuckDrop()
    {
        // Picked an hour early: too early. Picked 5 minutes in: the winner, and stronger than one before puck drop.
        Assert.Null(Award(Candidates([Pick(Sam, 1, minute: 0, startMinute: 60)]), LobbyAwards.BuzzerBeater));

        var late = Pick(Alex, 1, minute: 65, startMinute: 60);
        var award = Award(Candidates([Pick(Sam, 1, minute: 55, startMinute: 60), late, Pick(Jo, 0, minute: 70, startMinute: 60)]), LobbyAwards.BuzzerBeater)!;
        Assert.Equal((late, 5), (award.Pick, award.Value));
        Assert.True(award.Strength > Award(Candidates([Pick(Sam, 1, minute: 55, startMinute: 60)]), LobbyAwards.BuzzerBeater)!.Strength);
    }

    [Fact]
    public void IceColdNeedsSeveralPicksAndNoGoals()
    {
        Assert.Null(Award(Candidates([Pick(Sam, 0)]), LobbyAwards.IceCold));
        Assert.Null(Award(Candidates([Pick(Sam, 0), Pick(Sam, 1)]), LobbyAwards.IceCold));

        var cold = Award(Candidates([Pick(Sam, 0), Pick(Sam, 0), Pick(Alex, 0), Pick(Alex, 0), Pick(Alex, 0)]), LobbyAwards.IceCold)!;
        Assert.Equal((Alex.Id, 3), (cold.MemberId, cold.Value));
    }

    // --- Drinks ---

    [Fact]
    public void BullyGaveTheMostDrinksToOnePerson()
    {
        Assert.Null(Award(Candidates(drinks: [.. Gave(Sam, Alex, 1), .. Gave(Sam, Jo, 1)]), LobbyAwards.Bully));

        var bully = Award(Candidates(drinks: [.. Gave(Sam, Alex, 3), .. Gave(Jo, Alex, 2)]), LobbyAwards.Bully)!;
        Assert.Equal((Sam.Id, Alex.Id, 3), (bully.MemberId, bully.OtherMemberId!.Value, bully.Value));
    }

    [Fact]
    public void RivalryHasToGoBothWays()
    {
        Assert.Null(Award(Candidates(drinks: [.. Gave(Sam, Alex, 5), .. Gave(Alex, Sam, 1)]), LobbyAwards.Rivalry));

        var rivalry = Award(Candidates(drinks: [.. Gave(Sam, Alex, 2), .. Gave(Alex, Sam, 4), .. Gave(Jo, Sam, 5)]), LobbyAwards.Rivalry)!;
        Assert.Equal((Alex.Id, Sam.Id, 4, 2), (rivalry.MemberId, rivalry.OtherMemberId!.Value, rivalry.Value, rivalry.OtherValue));
    }

    [Fact]
    public void SpreadingTheLoveGaveToTheMostPeople()
    {
        Assert.Null(Award(Candidates(drinks: [.. Gave(Sam, Alex, 3), .. Gave(Sam, Jo, 3)]), LobbyAwards.SpreadingTheLove));

        var love = Award(Candidates(drinks: [.. Gave(Sam, Alex, 1), .. Gave(Sam, Jo, 1), .. Gave(Sam, Bot, 1)]), LobbyAwards.SpreadingTheLove)!;
        Assert.Equal((Sam.Id, 3), (love.MemberId, love.Value));
    }

    [Fact]
    public void ThirstiestTookTheMostAndTiesGoByName()
    {
        var thirstiest = Award(Candidates(drinks: [.. Gave(Sam, Jo, 2), .. Gave(Sam, Alex, 2)]), LobbyAwards.Thirstiest)!;

        Assert.Equal((Alex.Id, 2), (thirstiest.MemberId, thirstiest.Value));
    }

    [Fact]
    public void DesignatedDriverTookNoneOnABusyNight()
    {
        Assert.Null(Award(Candidates(drinks: [.. Gave(Sam, Alex, 2)]), LobbyAwards.DesignatedDriver)); // a quiet night

        // Sam and Jo took none; Sam gave more. The bot isn't eligible.
        var driver = Award(Candidates(drinks: [.. Gave(Sam, Alex, 4), .. Gave(Jo, Alex, 1), .. Gave(Alex, Bot, 1)]), LobbyAwards.DesignatedDriver)!;
        Assert.Equal((Sam.Id, 6), (driver.MemberId, driver.Value));
    }

    [Fact]
    public void HoarderHeldDrinksLongestBeforeHandingThemOut()
    {
        Assert.Null(Award(Candidates(drinks: [.. Gave(Sam, Alex, 3, heldMinutes: 2)]), LobbyAwards.Hoarder));

        var hoarder = Award(Candidates(drinks: [.. Gave(Sam, Alex, 2, heldMinutes: 10), .. Gave(Jo, Alex, 2, heldMinutes: 30)]), LobbyAwards.Hoarder)!;
        Assert.Equal((Jo.Id, 30), (hoarder.MemberId, hoarder.Value));
    }

    [Fact]
    public void DrinksWithMembersWhoLeftDoNotCount()
    {
        var gone = new RecapMember(Guid.NewGuid(), "Gone");

        var awards = Candidates(drinks: [.. Gave(Sam, gone, 5), .. Gave(gone, Jo, 5)]);

        Assert.Null(Award(awards, LobbyAwards.Thirstiest));
        Assert.Null(Award(awards, LobbyAwards.Bully));
    }

    // --- Chat, bots, achievements ---

    [Fact]
    public void ChatterboxIsWellAheadOfEveryoneElse()
    {
        Assert.Null(Award(Candidates(messages: new() { [Sam.Id] = 12, [Alex.Id] = 10 }), LobbyAwards.Chatterbox));
        Assert.Null(Award(Candidates(messages: new() { [Bot.Id] = 40 }), LobbyAwards.Chatterbox));

        Assert.Equal(Sam.Id, Award(Candidates(messages: new() { [Sam.Id] = 15, [Alex.Id] = 4 }), LobbyAwards.Chatterbox)!.MemberId);
    }

    [Fact]
    public void SilentAssassinGaveTheMostWithoutAWord()
    {
        var drinks = Gave(Jo, Alex, 3);

        Assert.Null(Award(Candidates(drinks: drinks), LobbyAwards.SilentAssassin)); // nobody was chatting
        Assert.Equal(Jo.Id, Award(Candidates(drinks: drinks, messages: new() { [Sam.Id] = 6 }), LobbyAwards.SilentAssassin)!.MemberId);
        Assert.Null(Award(Candidates(drinks: drinks, messages: new() { [Sam.Id] = 6, [Jo.Id] = 1 }), LobbyAwards.SilentAssassin));
    }

    [Fact]
    public void BeatenByARobotWhenABotGaveTheMost()
    {
        Assert.Null(Award(Candidates(drinks: [.. Gave(Bot, Sam, 2), .. Gave(Alex, Sam, 2)]), LobbyAwards.BeatenByARobot));

        Assert.Equal(Bot.Id, Award(Candidates(drinks: [.. Gave(Bot, Sam, 3), .. Gave(Alex, Sam, 2)]), LobbyAwards.BeatenByARobot)!.MemberId);
    }

    [Fact]
    public void OverAchieverIsTheRarestAchievementEarnedThatNight()
    {
        var rare = new RecapAchievement(Jo.Id, "Back-to-Back", "Play three nights in a row", 0.02);

        var award = Award(Candidates(achievements: [new RecapAchievement(Sam.Id, "Rookie", "Join a lobby", 0.9), rare]), LobbyAwards.OverAchiever)!;

        Assert.Equal((Jo.Id, rare), (award.MemberId, award.Achievement));
        Assert.True(award.Strength >= LobbyAwards.Notable);
    }

    // --- Selection ---

    private static LobbyAward Candidate(string key, RecapMember member, double strength) => new(key, member.Id, strength);

    [Fact]
    public void SelectionKeepsNotableAwardsAndTopsUpToTheMinimum()
    {
        var weak = new[] { Candidate("a", Sam, 0.1), Candidate("b", Alex, 0.25), Candidate("c", Jo, 0.4), Candidate("d", Bot, -0.2) };

        var chosen = LobbyAwards.Select(weak, seed: 1);

        Assert.Equal(LobbyAwards.MinAwards, chosen.Count);
        Assert.DoesNotContain(chosen, a => a.Key == "d"); // the weakest, more than the jitter away from the others
    }

    [Fact]
    public void SelectionStopsAtTheMaximumAndSpreadsAwardsAcrossMembers()
    {
        var many = Enumerable.Range(0, 12).Select(i => Candidate($"s{i}", Sam, 0.9)).Append(Candidate("alex", Alex, 0.55)).ToList();

        var chosen = LobbyAwards.Select(many, seed: 1);

        Assert.Contains(chosen, a => a.Key == "alex"); // Alex's one award comes before Sam's second
        Assert.Equal(LobbyAwards.MaxAwardsPerMember, chosen.Count(a => a.MemberId == Sam.Id));
        Assert.True(chosen.Count <= LobbyAwards.MaxAwards);
    }

    [Fact]
    public void TheSameLobbyAlwaysGetsTheSameAwards()
    {
        var close = Enumerable.Range(0, 10).Select(i => Candidate($"k{i}", new RecapMember(Guid.NewGuid(), $"M{i}"), 0.6)).ToList();

        var first = LobbyAwards.Select(close, seed: 42).Select(a => a.Key).ToList();

        Assert.Equal(first, LobbyAwards.Select(close, seed: 42).Select(a => a.Key));
        Assert.Equal(LobbyAwards.MaxAwards, first.Count);
    }
}
