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

    /// <summary>
    /// Whether the user who fetched the lobby can remove anyone's pick at any time (host or site admin).
    /// Depends on who's asking, so it's only set by <see cref="Management.GetLobbyByCodeQueryHandler"/>.
    /// </summary>
    public bool CanManageAllPicks { get; set; }
}