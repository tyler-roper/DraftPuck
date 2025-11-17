namespace DraftPuck.Application.Features.Achievements.Rules;

public class FirstDrinkAssignedRule(ILogger<FirstDrinkAssignedRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "first_rounder";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        return await dbContext.Drinks.AnyAsync(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id && d.RecipientLobbyMemberId != null);
    }
}