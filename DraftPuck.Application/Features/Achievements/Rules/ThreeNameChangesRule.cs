namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Change your name 3 times in a single lobby": counted from the lobby's name-change events.</summary>
public class ThreeNameChangesRule(ILogger<ThreeNameChangesRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "incognito_mode";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.NameChanged];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var memberIds = await dbContext.LobbyMembers
            .Where(lm => lm.UserId == user.Id)
            .Select(lm => (Guid?)lm.Id)
            .ToListAsync();

        return await dbContext.LobbyEvents
            .Where(e => e.LobbyEventType == LobbyEventType.UserNameChanged && memberIds.Contains(e.LobbyMemberId))
            .GroupBy(e => e.LobbyMemberId)
            .AnyAsync(g => g.Count() >= 3);
    }
}
