namespace DraftPuck.Application.Features.Lobbies.Management;

public class CreateLobbyCommand : IRequest<LobbyDto>
{
    public string Name { get; set; } = null!;
    public int PicksPerTeam { get; set; }
    public bool IsBotAutoPickingEnabled { get; set; } = false;
    public List<BotDto> Bots { get; set; } = [];
    public List<int> GameIds { get; set; } = [];
    public Guid CreatorUserId { get; set; }
}