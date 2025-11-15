namespace DraftPuck.Shared.Lobbies;

public partial class LobbyMemberPickEntity
{
    public Guid Id { get; set; }

    public Guid LobbyMemberId { get; set; }

    public int PlayerId { get; set; }

    public int GameId { get; set; }

    public int TeamId { get; set; }

    public DateTime Created { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<DrinkEntity> Drinks { get; } = [];

    public virtual LobbyMemberEntity LobbyMember { get; set; } = null!;
}
