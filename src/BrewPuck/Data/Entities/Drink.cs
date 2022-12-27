namespace BrewPuck.Data;

public partial class Drink
{
    public Guid Id { get; set; }

    public Guid LobbyMemberPickId { get; set; }

    public Guid? RecipientLobbyMemberId { get; set; }

    public int EventId { get; set; }

    public virtual LobbyMemberPick LobbyMemberPick { get; set; } = null!;

    public virtual LobbyMember? RecipientLobbyMember { get; set; }
}
