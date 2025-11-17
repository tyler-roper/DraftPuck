namespace DraftPuck.Shared.Games;

public class PlayerDto
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int Number { get; set; }
    public string Position { get; set; } = null!;
    public int GamesPlayed { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Points { get; set; }
    public string Headshot { get; set; } = null!;
}
