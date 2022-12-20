namespace BrewPuck.Data;

public partial class Player
{
    public long Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Number { get; set; } = null!;

    public string? Position { get; set; }

    public int TeamId { get; set; }

    public virtual ICollection<LobbyMemberPick> LobbyMemberPicks { get; } = new List<LobbyMemberPick>();

    public virtual Team Team { get; set; } = null!;
}
