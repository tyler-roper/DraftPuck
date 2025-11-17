namespace DraftPuck.Application.Features.Achievements.Rules;

public class FiveDrinksGivenRule(ILogger<FiveDrinksGivenRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "the_bartender";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        return await dbContext.Drinks
                .Where(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id && d.RecipientLobbyMemberId != null)
                .GroupBy(d => d.LobbyMemberPick.LobbyMember.LobbyId)
                .Select(g => g.Count())
                .AnyAsync(count => count >= 5);
    }
}