namespace DraftPuck.Data;

public partial class LobbyMember
{
    public Guid Id { get; set; }

    public Guid LobbyId { get; set; }

    public Guid UserId { get; set; }

    public DateTime Joined { get; set; }

    public string Name { get; set; } = null!;

    public bool IsBot { get; set; } = false;

    public BotPickStyle? BotPickStyle { get; set; }

    public virtual ICollection<Drink> Drinks { get; } = new List<Drink>();

    public virtual Lobby Lobby { get; set; } = null!;

    public virtual ICollection<LobbyMemberPick> LobbyMemberPicks { get; } = new List<LobbyMemberPick>();

    public virtual User User { get; set; } = null!;
}
