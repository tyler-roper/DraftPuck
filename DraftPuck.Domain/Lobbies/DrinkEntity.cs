namespace DraftPuck.Domain.Lobbies;

public partial class DrinkEntity
{
    public Guid Id { get; set; }

    public Guid LobbyMemberPickId { get; set; }

    public Guid? RecipientLobbyMemberId { get; set; }

    public int EventId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Assigned { get; set; }

    /// <summary>What the member said when handing the drink out, if anything.</summary>
    public string? Message { get; set; }

    public virtual LobbyMemberPickEntity LobbyMemberPick { get; set; } = null!;

    public virtual LobbyMemberEntity? RecipientLobbyMember { get; set; }
}
