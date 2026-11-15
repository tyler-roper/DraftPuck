namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>
/// "Give out the most drinks in a lobby with at least 5 human players." Decided when the lobby ends (deactivated at
/// the daily cutoff), against every member's count, bots included. A tie for the most counts.
/// </summary>
public class MostDrinksGivenRule(ILogger<MostDrinksGivenRule> logger) : BaseAchievementRule(logger)
{
    private const int MinimumHumans = 5;

    public override string UniqueIdentifier => "the_cup";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyEnded];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var endedMemberships = await dbContext.LobbyMembers
            .Where(lm => lm.UserId == user.Id && !lm.IsRemoved && !lm.Lobby.IsActive
                && lm.Lobby.LobbyMembers.Count(m => !m.IsBot && !m.IsRemoved) >= MinimumHumans)
            .Select(lm => new { lm.Id, lm.LobbyId })
            .ToListAsync();

        foreach (var membership in endedMemberships)
        {
            var givenByMember = await dbContext.Drinks
                .Where(d => d.RecipientLobbyMemberId != null && d.LobbyMemberPick.LobbyMember.LobbyId == membership.LobbyId)
                .GroupBy(d => d.LobbyMemberPick.LobbyMemberId)
                .Select(g => new { MemberId = g.Key, Count = g.Count() })
                .ToListAsync();

            var mine = givenByMember.FirstOrDefault(g => g.MemberId == membership.Id)?.Count ?? 0;
            if (mine > 0 && givenByMember.All(g => g.Count <= mine))
                return true;
        }

        return false;
    }
}
