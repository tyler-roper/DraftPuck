namespace DraftPuck.Core.Models;

public class PlayerSummary
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int Number { get; set; }
    public string Position { get; set; } = null!;
}
