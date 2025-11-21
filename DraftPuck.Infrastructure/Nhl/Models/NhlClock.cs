namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlClock
{
    public string TimeRemaining { get; set; } = null!;
    public int SecondsRemaining { get; set; }
    public bool Running { get; set; }
    public bool InIntermission { get; set; }
}
