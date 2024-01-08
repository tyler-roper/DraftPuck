namespace DraftPuck.Api.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string Abbreviation { get; set; } = null!;
    public string Logo { get; set; } = null!;
    public string DarkLogo { get; set; } = null!;
}
