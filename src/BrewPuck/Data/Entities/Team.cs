namespace BrewPuck.Data;

public partial class Team
{
    public int Id { get; set; }

    public string Abbreviation { get; set; } = null!;

    public string TeamName { get; set; } = null!;

    public string LocationName { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public virtual ICollection<Game> GameAwayTeams { get; } = new List<Game>();

    public virtual ICollection<Game> GameHomeTeams { get; } = new List<Game>();

    public virtual ICollection<Player> Players { get; } = new List<Player>();
}
