using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Web.Features.Lobbies;

public class CreateLobbyRequestDto
{
    public string Name { get; set; } = null!;
    public int PicksPerTeam { get; set; }
    public bool IsBotAutoPickingEnabled { get; set; } = false;
    public List<BotDto> Bots { get; set; } = [];
    public List<int> GameIds { get; set; } = [];
}
