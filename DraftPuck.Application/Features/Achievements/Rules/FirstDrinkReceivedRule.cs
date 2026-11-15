namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Receive your very first drink."</summary>
public class FirstDrinkReceivedRule(ILogger<FirstDrinkReceivedRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "initiated";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => dbContext.Drinks.AnyAsync(d => d.RecipientLobbyMember != null && d.RecipientLobbyMember.UserId == user.Id);
}
