namespace DraftPuck.Application.Features.Achievements.Rules;

public class ImmediatePickScoreRule(ILogger<ImmediatePickScoreRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "light_the_lamp";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAwarded];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var userDrinks = await dbContext.Drinks
            .Where(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id)
            .Select(d => new
            {
                DrinkAwarded = d.Created,
                PickMade = d.LobbyMemberPick.Created
            })
            .ToListAsync();

        return userDrinks.Any(ud => ud.DrinkAwarded - ud.PickMade <= TimeSpan.FromSeconds(60));
    }
}