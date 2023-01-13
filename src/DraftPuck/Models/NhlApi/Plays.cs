namespace DraftPuck.Models.NhlApi
{
public class Plays
    {
        public List<Play> AllPlays { get; set; } = null!;
        public List<int> ScoringPlays { get; set; } = null!;
        public List<int> PenaltyPlays { get; set; } = null!;
        public List<PlayIndices> PlaysByPeriod { get; set; } = null!;
        public Play CurrentPlay { get; set; } = null!;
    }
}