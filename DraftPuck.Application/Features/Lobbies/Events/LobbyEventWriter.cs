using System.Net;

namespace DraftPuck.Application.Features.Lobbies.Events;

/// <summary>
/// Adds an event to a lobby's feed and announces it (<see cref="LobbyEventCreatedNotification"/>), which pushes it to
/// the lobby's clients.
/// </summary>
public class LobbyEventWriter(IDbContext dbContext, IMediator mediator)
{
    public async Task<LobbyEventEntity> AddAsync(Guid lobbyId, string joinCode, LobbyEventType eventType, CancellationToken ct,
        Guid? lobbyMemberId = null, int? gameEventId = null, int? gameId = null, Guid? lobbyMember2Id = null, int? playerId = null,
        int? player2Id = null, int? teamId = null, string? title = null, string? text = null)
    {
        var lobbyEvent = new LobbyEventEntity
        {
            TimeUtc = DateTime.UtcNow,
            Title = title ?? LobbyEventTexts.GetTitle(eventType),
            Text = text ?? LobbyEventTexts.GetText(eventType),
            GameEventId = gameEventId,
            GameId = gameId,
            LobbyId = lobbyId,
            LobbyEventType = eventType,
            LobbyMemberId = lobbyMemberId,
            LobbyMember2Id = lobbyMember2Id,
            PlayerId = playerId,
            Player2Id = player2Id,
            TeamId = teamId
        };

        dbContext.LobbyEvents.Add(lobbyEvent);
        await dbContext.SaveChangesAsync(ct);

        await mediator.Publish(new LobbyEventCreatedNotification(lobbyEvent, joinCode), ct);
        return lobbyEvent;
    }

    /// <summary>A name for event text, which the client renders as HTML: user-supplied names must be encoded.</summary>
    public static string Bold(string name) => $"<strong>{WebUtility.HtmlEncode(name)}</strong>";
}
