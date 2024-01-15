namespace Draftpuck.NhlApi.Models;

public class NhlTeamSituation
{
    public string Abbrev { get; set; } = null!;
    public List<string> SituationDescriptions { get; set; } = new();
    public int Strength { get; set; }
}
