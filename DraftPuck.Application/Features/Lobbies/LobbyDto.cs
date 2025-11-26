using DraftPuck.Application.Features.Lobbies.Members;

namespace DraftPuck.Application.Features.Lobbies;

public class LobbyDto
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public string JoinCode { get; set; } = null!;
    public DateTime Created { get; set; }
    public Guid CreatedBy { get; set; }
    public int PicksPerTeam { get; set; }
    public bool IsBotAutoPickingEnabled { get; set; }
    public List<int> GameIds { get; set; } = [];
    public List<LobbyMemberDto> Members { get; set; } = [];
}