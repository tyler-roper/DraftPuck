namespace DraftPuck.Application.Features.Achievements.Rules;

public class OneBotDrinkReceivedRule(ILogger<OneBotDrinkReceivedRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "pinged";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        return await dbContext.Drinks
                .Where(d => d.RecipientLobbyMember != null && d.RecipientLobbyMember.UserId == user.Id)
                .AnyAsync(d => d.LobbyMemberPick.LobbyMember.IsBot);
    }
}