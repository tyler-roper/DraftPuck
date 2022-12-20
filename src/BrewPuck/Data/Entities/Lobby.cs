namespace BrewPuck.Data;

public partial class Lobby
{
    public Guid Id { get; set; }

    public string JoinCode { get; set; } = null!;

    public int Status { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<LobbyMember> LobbyMembers { get; } = new List<LobbyMember>();
}
