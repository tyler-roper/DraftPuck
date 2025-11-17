using DraftPuck.Shared.Users;

namespace DraftPuck.Shared.Lobbies;

public partial class LobbyEntity
{
    public Guid Id { get; set; }

    public bool IsActive { get; set; } = true;

    public string JoinCode { get; set; } = null!;

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public int PicksPerTeam { get; set; }

    public bool IsBotAutoPickingEnabled { get; set; } = true;

    public List<int> GameIds { get; set; } = [];

    public virtual UserEntity CreatedByUser { get; set; } = null!;

    public virtual ICollection<LobbyMemberEntity> LobbyMembers { get; } = [];
}
