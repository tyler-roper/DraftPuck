namespace DraftPuck.Shared.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public bool IsBot { get; set; } = false;

    public virtual ICollection<LobbyMember> LobbyMembers { get; } = new List<LobbyMember>();

    public virtual ICollection<Lobby> CreatedLobbies { get; } = new List<Lobby>();
}
