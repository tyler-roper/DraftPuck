namespace DraftPuck.Domain.Lobbies;

public partial class DrinkEntity
{
    public Guid Id { get; set; }

    public Guid LobbyMemberPickId { get; set; }

    public Guid? RecipientLobbyMemberId { get; set; }

    public int EventId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Assigned { get; set; }

    /// <summary>
    /// When its goal was reassigned or called back after the drink had been given to someone. The drink stays (it may
    /// have been drunk), but no longer counts as the pick's drink for that goal, so the goal can award again if it returns.
    /// </summary>
    public DateTime? Invalidated { get; set; }

    /// <summary>What the member said when handing the drink out, if anything.</summary>
    public string? Message { get; set; }

    public virtual LobbyMemberPickEntity LobbyMemberPick { get; set; } = null!;

    public virtual LobbyMemberEntity? RecipientLobbyMember { get; set; }
}
