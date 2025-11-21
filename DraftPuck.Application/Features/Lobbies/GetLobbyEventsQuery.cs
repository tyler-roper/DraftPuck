namespace DraftPuck.Application.Features.Lobbies;

public class GetLobbyEventsQuery : IRequest<IEnumerable<LobbyEventDto>>
{
    public Guid LobbyId { get; set; }
    public Guid UserId { get; set; }
}