namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>
/// "Give out 3 drinks without being assigned any in return": during a lobby, you've given out 3 drinks in it and
/// nobody has given you one back yet. Checked whenever you give a drink.
/// </summary>
public class InPlainSightRule(ILogger<InPlainSightRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "in_plain_sight";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var drinks = dbContext.Drinks;
        return dbContext.LobbyMembers
            .Where(lm => lm.UserId == user.Id && !lm.IsRemoved)
            .AnyAsync(lm =>
                lm.LobbyMemberPicks.SelectMany(p => p.Drinks).Count(d => d.RecipientLobbyMemberId != null) >= 3 &&
                !drinks.Any(d => d.RecipientLobbyMemberId == lm.Id));
    }
}
