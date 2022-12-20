namespace BrewPuck.Data;

public partial class Person
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime Created { get; set; }

    public virtual ICollection<LobbyMember> LobbyMembers { get; } = new List<LobbyMember>();
}
