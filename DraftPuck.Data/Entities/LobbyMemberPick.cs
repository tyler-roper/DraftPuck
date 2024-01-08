namespace DraftPuck.Data.Entities;

public partial class LobbyMemberPick
{
    public Guid Id { get; set; }

    public Guid LobbyMemberId { get; set; }

    public int PlayerId { get; set; }

    public int GameId { get; set; }

    public int TeamId { get; set; }

    public DateTime Created { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Drink> Drinks { get; } = new List<Drink>();

    public virtual LobbyMember LobbyMember { get; set; } = null!;
}
