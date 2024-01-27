namespace Draftpuck.NhlApi.Models;

public class NhlGameLanding : NhlGameBase
{
    public NhlGameSummary Summary { get; set; } = null!;
    public NhlSituation Situation { get; set; } = null!;
}
