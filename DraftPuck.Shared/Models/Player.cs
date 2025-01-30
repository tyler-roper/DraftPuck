namespace DraftPuck.Shared.Models;

public class Player
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
}
