using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Management;

namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>The hockey days (rolling over at the schedule's daily cutoff) the user has been in a lobby on.</summary>
internal static class HockeyDays
{
    public static async Task<List<DateTime>> PlayedAsync(IDbContext dbContext, Guid userId)
    {
        var created = await dbContext.LobbyMembers
            .Where(lm => lm.UserId == userId && !lm.IsRemoved)
            .Select(lm => lm.Lobby.Created)
            .ToListAsync();

        return [.. created.Select(c => GameProcessingHelpers.AdjustDateTimeForCutoff(c).Date).Distinct().Order()];
    }
}

/// <summary>"Play in lobbies on 3 nights in a row."</summary>
public class BackToBackRule(ILogger<BackToBackRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "back_to_back";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyJoined, AchievementTriggerType.LobbyCreated];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var days = await HockeyDays.PlayedAsync(dbContext, user.Id);
        for (var i = 0; i + 2 < days.Count; i++)
            if (days[i + 2] == days[i].AddDays(2))
                return true;

        return false;
    }
}

/// <summary>"Play in lobbies on 25 different nights."</summary>
public class SeasonTicketHolderRule(ILogger<SeasonTicketHolderRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "season_ticket_holder";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyJoined, AchievementTriggerType.LobbyCreated];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => (await HockeyDays.PlayedAsync(dbContext, user.Id)).Count >= 25;
}

/// <summary>"Send 50 chat messages in one lobby."</summary>
public class ChirpMachineRule(ILogger<ChirpMachineRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "chirp_machine";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.MessageSent];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => dbContext.LobbyMembers
            .Where(lm => lm.UserId == user.Id)
            .AnyAsync(lm => lm.Messages.Count(m => !m.IsDeleted) >= 50);
}

/// <summary>"Host a lobby with the maximum number of bots."</summary>
public class BotArmyRule(ILogger<BotArmyRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "bot_army";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyCreated, AchievementTriggerType.HostedLobbyJoined];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => dbContext.Lobbies
            .Where(l => l.CreatedBy == user.Id)
            .AnyAsync(l => l.LobbyMembers.Count(m => m.IsBot && !m.IsRemoved) >= LobbyRules.MaxBots);
}

/// <summary>"Play in a lobby during the playoffs."</summary>
public class PlayoffBeardRule(ILogger<PlayoffBeardRule> logger, IGameCache gameCache) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "playoff_beard";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyJoined, AchievementTriggerType.LobbyCreated];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var gameIdLists = await dbContext.LobbyMembers
            .Where(lm => lm.UserId == user.Id && !lm.IsRemoved && lm.Lobby.IsActive)
            .Select(lm => lm.Lobby.GameIds)
            .ToListAsync();

        foreach (var gameId in gameIdLists.SelectMany(ids => ids).Distinct())
            if (await gameCache.GetGameByIdAsync(gameId) is { GameType: GameType.Playoffs })
                return true;

        return false;
    }
}

/// <summary>"Have one of your picks scratched from the lineup": read from the lobby's pick-scratched events.</summary>
public class HealthyScratchRule(ILogger<HealthyScratchRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "healthy_scratch";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.PickScratched];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var memberIds = await dbContext.LobbyMembers.Where(lm => lm.UserId == user.Id).Select(lm => (Guid?)lm.Id).ToListAsync();
        return await dbContext.LobbyEvents.AnyAsync(e => e.LobbyEventType == LobbyEventType.PickScratched && memberIds.Contains(e.LobbyMemberId));
    }
}
