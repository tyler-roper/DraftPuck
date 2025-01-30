namespace DraftPuck.Shared.Models;

public class GameSummary
{
    public int Id { get; set; }
    public DateTime DateTime { get; set; }
    public GameType GameType { get; set; }
    public GameState GameState { get; set; }
}
