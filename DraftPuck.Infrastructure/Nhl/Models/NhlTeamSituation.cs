namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlTeamSituation
{
    public string Abbrev { get; set; } = null!;
    public List<string> SituationDescriptions { get; set; } = [];
    public int Strength { get; set; }
}
