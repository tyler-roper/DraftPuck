namespace DraftPuck.Application.Features.Achievements.Rules;

public class ThreeGoalsForOnePickRule(ILogger<ThreeGoalsForOnePickRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "mad_hatter";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAwarded];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var hatTrickPicks = await dbContext.LobbyMemberPicks
            .Where(p => p.LobbyMember.UserId == user.Id)
            .Select(p => new
            {
                PickCreatedTime = p.Created,
                DrinkCount = p.Drinks.Count(),
                Drinks = p.Drinks.Select(d => d.Created)
            })
            .Where(p => p.DrinkCount >= 3)
            .ToListAsync();

        if (hatTrickPicks.Count == 0)
            return false;

        foreach (var pick in hatTrickPicks)
        {
            var postPickGoals = pick.Drinks.Count(drinkTime => drinkTime > pick.PickCreatedTime);

            if (postPickGoals >= 3)
                return true;
        }

        return false;
    }
}