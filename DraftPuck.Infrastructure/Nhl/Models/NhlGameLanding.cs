namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlGameLanding : NhlGameBase
{
    public NhlGameSummary Summary { get; set; } = null!;
    public NhlSituation Situation { get; set; } = null!;
}
