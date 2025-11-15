namespace DraftPuck.Shared.Games;

public class GameTeamDto : TeamDto
{
    public int Score { get; set; }
    public List<PlayerDto> Roster { get; set; } = [];
    public List<TeamSituation> Situations { get; set; } = [];
    public int Strength { get; set; }
}
