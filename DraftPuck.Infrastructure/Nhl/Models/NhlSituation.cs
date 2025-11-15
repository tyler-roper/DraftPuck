namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlSituation
{
    public NhlTeamSituation HomeTeam { get; set; } = null!;
    public NhlTeamSituation AwayTeam { get; set; } = null!;
    public string SituationCode { get; set; } = null!;
    public string TimeRemaining { get; set; } = null!;
    public int SecondsRemaining { get; set; }
}
