namespace BrewPuck.Data;

public partial class LobbyMember
{
    public Guid Id { get; set; }

    public Guid LobbyId { get; set; }

    public Guid PersonId { get; set; }

    public virtual ICollection<Drink> Drinks { get; } = new List<Drink>();

    public virtual Lobby Lobby { get; set; } = null!;

    public virtual ICollection<LobbyMemberPick> LobbyMemberPicks { get; } = new List<LobbyMemberPick>();

    public virtual Person Person { get; set; } = null!;
}
