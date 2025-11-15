namespace DraftPuck.Shared.Games;

public class PlayerSummaryDto
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int Number { get; set; }
    public string? Position { get; set; }
}
