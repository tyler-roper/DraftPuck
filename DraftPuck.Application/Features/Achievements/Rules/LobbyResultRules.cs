namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>Achievements decided when a lobby ends (deactivated at the daily cutoff), from the picks and drinks you finished with.</summary>
public abstract class LobbyResultRule(ILogger logger) : BaseAchievementRule(logger)
{
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyEnded];

    protected static IQueryable<LobbyMemberEntity> EndedMemberships(IDbContext dbContext, Guid userId)
        => dbContext.LobbyMembers.Where(lm => lm.UserId == userId && !lm.IsRemoved && !lm.Lobby.IsActive);
}

/// <summary>"Make every one of your picks in a lobby from the same team, and have at least one of them score."</summary>
public class HomerRule(ILogger<HomerRule> logger) : LobbyResultRule(logger)
{
    public override string UniqueIdentifier => "homer";

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => EndedMemberships(dbContext, user.Id).AnyAsync(lm =>
            lm.LobbyMemberPicks.Count(p => p.IsActive) >= 2 &&
            lm.LobbyMemberPicks.Where(p => p.IsActive).Select(p => p.TeamId).Distinct().Count() == 1 &&
            lm.LobbyMemberPicks.Any(p => p.IsActive && p.Drinks.Any()));
}

/// <summary>"Have every one of your picks in a lobby score (at least 3 picks)."</summary>
public class CleanSheetRule(ILogger<CleanSheetRule> logger) : LobbyResultRule(logger)
{
    public override string UniqueIdentifier => "clean_sheet";

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => EndedMemberships(dbContext, user.Id).AnyAsync(lm =>
            lm.LobbyMemberPicks.Count(p => p.IsActive) >= 3 &&
            lm.LobbyMemberPicks.Where(p => p.IsActive).All(p => p.Drinks.Any()));
}

/// <summary>"Make 4 or more picks in a lobby and have none of them score."</summary>
public class GooseEggRule(ILogger<GooseEggRule> logger) : LobbyResultRule(logger)
{
    public override string UniqueIdentifier => "goose_egg";

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => EndedMemberships(dbContext, user.Id).AnyAsync(lm =>
            lm.LobbyMemberPicks.Count(p => p.IsActive) >= 4 &&
            !lm.LobbyMemberPicks.Any(p => p.IsActive && p.Drinks.Any()));
}

/// <summary>"Finish a lobby with at least 5 human players without receiving a single drink."</summary>
public class DesignatedDriverRule(ILogger<DesignatedDriverRule> logger) : LobbyResultRule(logger)
{
    public override string UniqueIdentifier => "designated_driver";

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var drinks = dbContext.Drinks;
        return EndedMemberships(dbContext, user.Id).AnyAsync(lm =>
            lm.Lobby.LobbyMembers.Count(m => !m.IsBot && !m.IsRemoved) >= 5 &&
            !drinks.Any(d => d.RecipientLobbyMemberId == lm.Id));
    }
}
