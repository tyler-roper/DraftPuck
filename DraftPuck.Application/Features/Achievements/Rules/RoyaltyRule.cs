using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Create a lobby on 10 different days": hockey days, which roll over at the same daily cutoff as the schedule.</summary>
public class RoyaltyRule(ILogger<RoyaltyRule> logger) : BaseAchievementRule(logger)
{
    private const int Days = 10;

    public override string UniqueIdentifier => "royalty";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyCreated];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var created = await dbContext.Lobbies
            .Where(l => l.CreatedBy == user.Id)
            .Select(l => l.Created)
            .ToListAsync();

        return created.Select(c => GameProcessingHelpers.AdjustDateTimeForCutoff(c).Date).Distinct().Count() >= Days;
    }
}
