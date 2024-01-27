using DraftPuck.Shared.Enums;

namespace DraftPuck.Shared.Entities;

public partial class Lobby
{
    public Guid Id { get; set; }

    public string JoinCode { get; set; } = null!;

    public LobbyStatus Status { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public int PicksPerTeam { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<LobbyMember> LobbyMembers { get; } = new List<LobbyMember>();
}
