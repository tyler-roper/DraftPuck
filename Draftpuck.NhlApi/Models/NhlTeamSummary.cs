namespace Draftpuck.NhlApi.Models;

public class NhlTeamSummary
{
    public int Id { get; set; }
    public NhlDefaultString Name { get; set; } = null!;
    public NhlDefaultString PlaceName { get; set; } = null!;
    public string Abbrev { get; set; } = null!;
    public string Logo { get; set; } = null!;
    public string DarkLogo { get; set; } = null!;
    public int Score { get; set; }
}
