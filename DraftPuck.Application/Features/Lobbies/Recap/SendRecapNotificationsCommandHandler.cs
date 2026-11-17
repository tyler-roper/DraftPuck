using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Games;
using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.Lobbies.Recap;

public class SendRecapNotificationsCommandHandler(IDbContext dbContext, IMediator mediator, IPushNotificationService pushService,
    IOptions<ApplicationOptions> appConfig, ILogger<SendRecapNotificationsCommandHandler> logger)
    : IRequestHandler<SendRecapNotificationsCommand>
{
    /// <summary>Noon in New York: people are up, and the night is still fresh.</summary>
    public const int LocalHour = 12;
    public static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public async Task Handle(SendRecapNotificationsCommand request, CancellationToken ct)
    {
        var now = appConfig.Value.CurrentTimeUtc;
        // The timer fires at both UTC hours noon Eastern can fall on (daylight saving moves it); only the right one sends.
        if (TimeZoneInfo.ConvertTimeFromUtc(now, Eastern).Hour != LocalHour) return;

        // Last night: the day's lobbies, which the morning cutoff has ended (DeactivateStaleLobbiesCommandHandler).
        var nightEnd = GameProcessingHelpers.AdjustDateTimeForCutoff(now).Date.AddHours(GameProcessingHelpers.CutoffHours);
        var nightStart = nightEnd.AddDays(-1);

        var lobbies = (await dbContext.Lobbies
                .AsNoTracking()
                .Where(l => !l.IsActive && l.Created >= nightStart && l.Created < nightEnd)
                .Select(l => new
                {
                    l.JoinCode,
                    Drinks = l.LobbyMembers.SelectMany(m => m.LobbyMemberPicks.Where(p => p.IsActive)).SelectMany(p => p.Drinks).Count(),
                    Members = l.LobbyMembers.Where(m => !m.IsRemoved && !m.IsBot).Select(m => new { m.Id, m.UserId }).ToList()
                })
                .ToListAsync(ct))
            // Only lobbies that saw some action have a recap worth a push.
            .Where(l => l.Drinks > 0)
            .ToList();
        if (lobbies.Count == 0) return;

        var userIds = lobbies.SelectMany(l => l.Members.Select(m => m.UserId)).Distinct().ToList();
        var users = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Devices)
            .Where(u => userIds.Contains(u.Id) && u.RecapNotificationPreference != NotificationPreference.None && u.Devices.Any())
            .ToListAsync(ct);
        if (users.Count == 0) return;

        // Achievements awarded quietly when the lobbies ended this morning (CheckAndAwardAllAchievementsCommandHandler).
        var recipientIds = users.Select(u => u.Id).ToList();
        var earned = (await dbContext.UserAchievements
                .AsNoTracking()
                .Where(ua => recipientIds.Contains(ua.UserId) && ua.DateEarned >= nightEnd && ua.DateEarned <= now)
                .OrderBy(ua => ua.DateEarned)
                .Select(ua => new { ua.UserId, ua.Achievement.FriendlyName })
                .ToListAsync(ct))
            .ToLookup(a => a.UserId, a => a.FriendlyName);

        var awardsByCode = new Dictionary<string, List<LobbyAwardDto>>();
        var sent = 0;
        foreach (var user in users)
        {
            // Someone in several lobbies gets one push, for the busiest.
            var lobby = lobbies.Where(l => l.Members.Any(m => m.UserId == user.Id)).MaxBy(l => l.Drinks)!;
            var memberId = lobby.Members.First(m => m.UserId == user.Id).Id;

            if (!awardsByCode.TryGetValue(lobby.JoinCode, out var awards))
                awardsByCode[lobby.JoinCode] = awards = await AwardsAsync(lobby.JoinCode, ct);

            var won = awards.Where(a => a.MemberId == memberId || a.OtherMemberId == memberId).Select(a => a.Title).ToList();
            var teaser = awards.FirstOrDefault(a => a.Key != LobbyAwards.Rivalry);
            var body = RecapPushText.Body(won, [.. earned[user.Id].Distinct()], teaser);

            try
            {
                sent += await pushService.SendRecapNotification(lobby.JoinCode, RecapPushText.Title, body, user.PushTokens(), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Couldn't send the recap push to user {UserId}.", user.Id);
            }
        }

        logger.LogInformation("Recap pushes sent to {Devices} devices, for {Lobbies} lobbies.", sent, lobbies.Count);
    }

    /// <summary>The lobby's awards (cached for a finished lobby); none if the recap can't be built, so the push still goes.</summary>
    private async Task<List<LobbyAwardDto>> AwardsAsync(string code, CancellationToken ct)
    {
        try
        {
            return (await mediator.Send(new GetLobbyRecapQuery { Code = code }, ct)).Awards;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't build lobby {Code}'s recap for its push.", code);
            return [];
        }
    }
}
