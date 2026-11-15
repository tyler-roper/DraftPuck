using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>One of the user's scoring picks, with the goal play and game it scored in.</summary>
public record ScoringPick(GameDto Game, PlayDto Goal, DateTime PickMade);

/// <summary>
/// Achievements about the goal itself (when, and how, it was scored). Each drink records its goal's play ID, and the
/// play is read from the cached game, so only recent goals (games still in the cache) are checked.
/// </summary>
public abstract class GoalMomentRule(ILogger logger, IGameCache gameCache) : BaseAchievementRule(logger)
{
    private static readonly TimeSpan Lookback = TimeSpan.FromDays(2);

    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAwarded];

    protected abstract bool Qualifies(ScoringPick pick);

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        foreach (var pick in await ScoringPicksAsync(dbContext, gameCache, user.Id))
            if (Qualifies(pick))
                return true;

        return false;
    }

    public static async Task<List<ScoringPick>> ScoringPicksAsync(IDbContext dbContext, IGameCache gameCache, Guid userId)
    {
        var since = DateTime.UtcNow - Lookback;
        var drinks = await dbContext.Drinks
            .Where(d => d.LobbyMemberPick.LobbyMember.UserId == userId && !d.LobbyMemberPick.LobbyMember.IsRemoved && d.Created >= since)
            .Select(d => new { d.EventId, d.LobbyMemberPick.GameId, d.LobbyMemberPick.PlayerId, PickMade = d.LobbyMemberPick.Created })
            .ToListAsync();

        var games = new Dictionary<int, GameDto?>();
        var picks = new List<ScoringPick>();
        foreach (var drink in drinks)
        {
            if (!games.TryGetValue(drink.GameId, out var game))
                games[drink.GameId] = game = await gameCache.GetGameByIdAsync(drink.GameId);

            var goal = game?.Plays.FirstOrDefault(p => p.Id == drink.EventId && p.Type == PlayType.Goal
                && p.PeriodType != PeriodType.Shootout && p.PrimaryPlayerId == drink.PlayerId);
            if (goal != null)
                picks.Add(new ScoringPick(game!, goal, drink.PickMade));
        }

        return picks;
    }

    /// <summary>The on-ice situation for the scoring team, from the NHL's situation code; null when it's missing.</summary>
    protected static (int Goalie, int Skaters, int OpponentGoalie, int OpponentSkaters)? Situation(ScoringPick pick)
    {
        var code = pick.Goal.SituationCode;
        if (code is not { Length: 4 } || !code.All(char.IsDigit)) return null;

        int Digit(int i) => code[i] - '0';
        var (awayGoalie, awaySkaters, homeSkaters, homeGoalie) = (Digit(0), Digit(1), Digit(2), Digit(3));
        return pick.Goal.PrimaryTeamId == pick.Game.HomeTeam.Id
            ? (homeGoalie, homeSkaters, awayGoalie, awaySkaters)
            : (awayGoalie, awaySkaters, homeGoalie, homeSkaters);
    }

    /// <summary>Seconds left in the period when the goal was scored ("mm:ss"); null if it can't be read.</summary>
    protected static int? SecondsRemaining(ScoringPick pick)
    {
        var parts = pick.Goal.TimeRemainingInPeriod?.Split(':');
        return parts is { Length: 2 } && int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var s) ? m * 60 + s : null;
    }
}

/// <summary>"Have one of your players score in overtime."</summary>
public class SuddenDeathRule(ILogger<SuddenDeathRule> logger, IGameCache gameCache) : GoalMomentRule(logger, gameCache)
{
    public override string UniqueIdentifier => "sudden_death";
    protected override bool Qualifies(ScoringPick pick) => pick.Goal.PeriodType == PeriodType.Overtime;
}

/// <summary>"Have one of your players score with under 10 seconds left in a period."</summary>
public class BuzzerBeaterRule(ILogger<BuzzerBeaterRule> logger, IGameCache gameCache) : GoalMomentRule(logger, gameCache)
{
    public override string UniqueIdentifier => "buzzer_beater";
    protected override bool Qualifies(ScoringPick pick) => SecondsRemaining(pick) is < 10;
}

/// <summary>"Have one of your players score the first goal of a game."</summary>
public class FirstBloodRule(ILogger<FirstBloodRule> logger, IGameCache gameCache) : GoalMomentRule(logger, gameCache)
{
    public override string UniqueIdentifier => "first_blood";
    protected override bool Qualifies(ScoringPick pick) => pick.Goal.HomeScore + pick.Goal.AwayScore == 1;
}

/// <summary>"Have one of your players score a power-play goal": more skaters than the other team, with their own goalie in net.</summary>
public class ManAdvantageRule(ILogger<ManAdvantageRule> logger, IGameCache gameCache) : GoalMomentRule(logger, gameCache)
{
    public override string UniqueIdentifier => "man_advantage";
    protected override bool Qualifies(ScoringPick pick) => Situation(pick) is { Goalie: 1 } s && s.Skaters > s.OpponentSkaters;
}

/// <summary>"Have one of your players score into an empty net."</summary>
public class EmptyNetterRule(ILogger<EmptyNetterRule> logger, IGameCache gameCache) : GoalMomentRule(logger, gameCache)
{
    public override string UniqueIdentifier => "empty_netter";
    protected override bool Qualifies(ScoringPick pick) => Situation(pick) is { OpponentGoalie: 0 };
}

/// <summary>"Pick a player after puck drop, and have them score."</summary>
public class LateCallUpRule(ILogger<LateCallUpRule> logger, IGameCache gameCache) : GoalMomentRule(logger, gameCache)
{
    public override string UniqueIdentifier => "late_call_up";
    protected override bool Qualifies(ScoringPick pick) => pick.PickMade > pick.Game.DateTime;
}

/// <summary>
/// "Have one of your players score the game-winning goal": checked when the game goes final. The winner is the goal
/// that put the winning team one ahead of the loser's final score. Games decided by a shootout have no game winner.
/// </summary>
public class GameWinnerRule(ILogger<GameWinnerRule> logger, IGameCache gameCache) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "game_winner";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.GameFinished];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        foreach (var pick in await GoalMomentRule.ScoringPicksAsync(dbContext, gameCache, user.Id))
            if (pick.Game.GameState == GameState.Final && GameWinningGoalId(pick.Game) == pick.Goal.Id)
                return true;

        return false;
    }

    public static int? GameWinningGoalId(GameDto game)
    {
        var goals = game.Plays.Where(p => p.Type == PlayType.Goal && p.PeriodType != PeriodType.Shootout).ToList();
        if (goals.Count == 0 || game.Plays.Any(p => p.PeriodType == PeriodType.Shootout)) return null;

        var last = goals[^1];
        if (last.HomeScore == last.AwayScore) return null;

        var homeWon = last.HomeScore > last.AwayScore;
        var losingScore = homeWon ? last.AwayScore : last.HomeScore;
        return goals.FirstOrDefault(g => (homeWon ? g.HomeScore : g.AwayScore) == losingScore + 1
            && g.PrimaryTeamId == (homeWon ? game.HomeTeam.Id : game.AwayTeam.Id))?.Id;
    }
}
