namespace DraftPuck.Data;

public partial class LobbyMemberPick
{
    public Guid Id { get; set; }

    public Guid LobbyMemberId { get; set; }

    public long PlayerId { get; set; }

    public long GamePk { get; set; }

    public int TeamId { get; set; }

    public DateTime Created { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Drink> Drinks { get; } = new List<Drink>();

    public virtual LobbyMember LobbyMember { get; set; } = null!;
}
