namespace DraftPuck.Application.Features.Lobbies.Management;

public class GetLobbyByCodeQuery : IRequest<LobbyDto>
{
    public string Code { get; set; } = null!;
}