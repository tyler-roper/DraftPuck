namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Give out 3 drinks in less than 3 minutes."</summary>
public class FullAutoRule(ILogger<FullAutoRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "full_auto";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var assigned = await dbContext.Drinks
            .Where(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id && d.RecipientLobbyMemberId != null && d.Assigned != null)
            .Select(d => d.Assigned!.Value)
            .OrderBy(t => t)
            .ToListAsync();

        for (var i = 0; i + 2 < assigned.Count; i++)
            if (assigned[i + 2] - assigned[i] < TimeSpan.FromMinutes(3))
                return true;

        return false;
    }
}
