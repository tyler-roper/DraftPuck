namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Give 5 drinks to the same person in one lobby."</summary>
public class TargetPracticeRule(ILogger<TargetPracticeRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "target_practice";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    // A recipient is a lobby member, so grouping by them is per lobby already.
    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => dbContext.Drinks
            .Where(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id && d.RecipientLobbyMemberId != null)
            .GroupBy(d => d.RecipientLobbyMemberId)
            .AnyAsync(g => g.Count() >= 5);
}

/// <summary>"Give drinks to 5 different people in one lobby."</summary>
public class SpreadTheWealthRule(ILogger<SpreadTheWealthRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "spread_the_wealth";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => dbContext.Drinks
            .Where(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id && d.RecipientLobbyMemberId != null)
            .GroupBy(d => d.LobbyMemberPick.LobbyMember.LobbyId)
            .AnyAsync(g => g.Select(d => d.RecipientLobbyMemberId).Distinct().Count() >= 5);
}

/// <summary>"Receive 10 or more drinks in one lobby."</summary>
public class BottomlessCupRule(ILogger<BottomlessCupRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "bottomless_cup";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => dbContext.Drinks
            .Where(d => d.RecipientLobbyMember != null && d.RecipientLobbyMember.UserId == user.Id)
            .GroupBy(d => d.RecipientLobbyMemberId)
            .AnyAsync(g => g.Count() >= 10);
}
