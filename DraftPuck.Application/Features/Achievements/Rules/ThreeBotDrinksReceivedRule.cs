namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Receive 3 drinks from Bots in a single lobby."</summary>
public class ThreeBotDrinksReceivedRule(ILogger<ThreeBotDrinksReceivedRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "random_number_generator";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => dbContext.Drinks
            .Where(d =>
                d.RecipientLobbyMember != null &&
                d.RecipientLobbyMember.UserId == user.Id &&
                !d.RecipientLobbyMember.IsRemoved &&
                d.LobbyMemberPick.LobbyMember.IsBot)
            .GroupBy(d => d.RecipientLobbyMember!.LobbyId)
            .AnyAsync(g => g.Count() >= 3);
}
