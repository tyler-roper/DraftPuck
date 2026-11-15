namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>
/// A drink already given out whose goal was reassigned or called back stays (it may have been drunk) and is recorded
/// as a "drink invalidated" lobby event with the giver as LobbyMemberId and the recipient as LobbyMember2Id.
/// </summary>
internal static class InvalidatedDrinks
{
    public static async Task<List<Guid?>> MemberIdsAsync(IDbContext dbContext, Guid userId)
        => await dbContext.LobbyMembers.Where(lm => lm.UserId == userId).Select(lm => (Guid?)lm.Id).ToListAsync();

    public static IQueryable<LobbyEventEntity> Events(IDbContext dbContext)
        => dbContext.LobbyEvents.Where(e => e.LobbyEventType == LobbyEventType.DrinkInvalidated);
}

/// <summary>"Have a drink assignment revoked due to an official scoring change": a drink you gave out was invalidated.</summary>
public class FalseAlarmRule(ILogger<FalseAlarmRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "false_alarm";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkInvalidated];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var memberIds = await InvalidatedDrinks.MemberIdsAsync(dbContext, user.Id);
        return await InvalidatedDrinks.Events(dbContext).AnyAsync(e => memberIds.Contains(e.LobbyMemberId));
    }
}

/// <summary>"Drink a beer for a goal that was later waved off by an official review": a drink you received was invalidated.</summary>
public class WavedOffRule(ILogger<WavedOffRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "waved_off";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkInvalidated];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var memberIds = await InvalidatedDrinks.MemberIdsAsync(dbContext, user.Id);
        return await InvalidatedDrinks.Events(dbContext).AnyAsync(e => memberIds.Contains(e.LobbyMember2Id));
    }
}
