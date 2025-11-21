namespace DraftPuck.Application.Features.Lobbies;

public class GetLobbyByCodeQuery : IRequest<LobbyDto>
{
    public string Code { get; set; } = null!;
}