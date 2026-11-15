namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Have two of your chosen players score in the same minute": two different players, within 60 seconds, in one lobby.</summary>
public class TwoGoalsInOneMinute(ILogger<TwoGoalsInOneMinute> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "hot_hand";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAwarded];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        // A drink is created when its goal is found, so its time is when the goal was scored (to within a poll).
        var goals = await dbContext.Drinks
            .Where(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id && !d.LobbyMemberPick.LobbyMember.IsRemoved)
            .Select(d => new { d.LobbyMemberPick.LobbyMember.LobbyId, d.LobbyMemberPick.PlayerId, d.Created })
            .ToListAsync();

        foreach (var lobbyGoals in goals.GroupBy(g => g.LobbyId))
        {
            var ordered = lobbyGoals.OrderBy(g => g.Created).ToList();
            for (var i = 0; i < ordered.Count; i++)
                for (var j = i + 1; j < ordered.Count && ordered[j].Created - ordered[i].Created <= TimeSpan.FromSeconds(60); j++)
                    if (ordered[j].PlayerId != ordered[i].PlayerId)
                        return true;
        }

        return false;
    }
}
