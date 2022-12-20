namespace BrewPuck.Data;

public partial class Game
{
    public long GamePk { get; set; }

    public string Type { get; set; } = null!;

    public DateTime Date { get; set; }

    public int HomeTeamId { get; set; }

    public int AwayTeamId { get; set; }

    public GameStatus StatusCode { get; set; }

    public virtual Team AwayTeam { get; set; } = null!;

    public virtual Team HomeTeam { get; set; } = null!;
}
