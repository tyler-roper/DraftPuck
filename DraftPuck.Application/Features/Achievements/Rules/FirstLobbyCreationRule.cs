namespace DraftPuck.Application.Features.Achievements.Rules;

public class FirstLobbyCreationRule(ILogger<FirstLobbyCreationRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "rookie_host";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyCreated];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var lobbyCount = await dbContext.Lobbies
            .CountAsync(l => l.CreatedBy == user.Id);

        return lobbyCount > 0;
    }
}