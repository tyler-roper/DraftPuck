namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlPlayerSummary
{
    public int TeamId { get; set; }
    public int PlayerId { get; set; }
    public NhlDefaultString FirstName { get; set; } = null!;
    public NhlDefaultString LastName { get; set; } = null!;
    public int SweaterNumber { get; set; }
    public string PositionCode { get; set; } = null!;
    public string Headshot { get; set; } = null!;
}
