namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Give and receive a drink from the same player within 8 minutes": a drink to someone and one back from them, either order.</summary>
public class DrinkExchangeRule(ILogger<DrinkExchangeRule> logger) : BaseAchievementRule(logger)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(8);

    public override string UniqueIdentifier => "8_bit";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var given = await dbContext.Drinks
            .Where(d => d.LobbyMemberPick.LobbyMember.UserId == user.Id && d.RecipientLobbyMemberId != null && d.Assigned != null)
            .Select(d => new { OtherMemberId = d.RecipientLobbyMemberId!.Value, Assigned = d.Assigned!.Value })
            .ToListAsync();
        if (given.Count == 0) return false;

        var received = await dbContext.Drinks
            .Where(d => d.RecipientLobbyMember != null && d.RecipientLobbyMember.UserId == user.Id && d.Assigned != null)
            .Select(d => new { OtherMemberId = d.LobbyMemberPick.LobbyMemberId, Assigned = d.Assigned!.Value })
            .ToListAsync();

        return given.Any(g => received.Any(r => r.OtherMemberId == g.OtherMemberId && (r.Assigned - g.Assigned).Duration() <= Window));
    }
}
