namespace DraftPuck.Application.Features.Achievements.Rules;

public class TwoGoalsInOneMinute(ILogger<TwoGoalsInOneMinute> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "hot_hand";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAwarded];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var activeLobbyMemberships = await dbContext.LobbyMembers
                .Where(lm => lm.UserId == user.Id && !lm.IsRemoved)
                .Select(lm => lm.LobbyId)
                .ToListAsync();

        if (activeLobbyMemberships.Count == 0) return false;

        foreach (var lobbyId in activeLobbyMemberships)
        {
            var allDrinksInLobby = await dbContext.Drinks
                .Where(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id &&
                            d.LobbyMemberPick.LobbyMember.LobbyId == lobbyId)
                .OrderBy(d => d.Created)
                .Select(d => d.Created)
                .ToListAsync();

            if (allDrinksInLobby.Count < 2)
                continue;

            for (var i = 1; i < allDrinksInLobby.Count; i++)
            {
                var timeDifference = allDrinksInLobby[i] - allDrinksInLobby[i - 1];
                if (timeDifference <= TimeSpan.FromSeconds(60))
                    return true;
            }
        }

        return false;
    }
}