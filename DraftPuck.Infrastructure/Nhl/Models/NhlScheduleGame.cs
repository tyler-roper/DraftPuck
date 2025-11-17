namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlScheduleGame
{
    public int Id { get; set; }
    public int Season { get; set; }
    public int GameType { get; set; }
    public DateTime StartTimeUTC { get; set; }
    public string GameState { get; set; } = null!;
    public string GameScheduleState { get; set; } = null!;
    public NhlTeamSummary AwayTeam { get; set; } = null!;
    public NhlTeamSummary HomeTeam { get; set; } = null!;
    public NhlPeriodDescriptor PeriodDescriptor { get; set; } = null!;
}
