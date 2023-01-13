namespace DraftPuck.Models.NhlApi
{
public class LiveData
    {
        public Plays Plays { get; set; } = null!;
        public LineScore Linescore { get; set; } = null!;
        public BoxScore Boxscore { get; set; } = null!;
        public Decisions Decisions { get; set; } = null!;
    }
}