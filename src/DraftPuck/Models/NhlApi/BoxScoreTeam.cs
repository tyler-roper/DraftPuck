namespace DraftPuck.Models.NhlApi
{
    public class BoxScoreTeam
    {
        public ExtendedTeamSummary Team { get; set; } = null!;
        public TeamStats TeamStats { get; set; } = null!;
        public List<long> Goalies { get; set; } = null!;
        public List<long> Skaters { get; set; } = null!;
        public List<long> OnIce { get; set; } = null!;
        public List<OnIcePlus> OnIcePlus { get; set; } = null!;
        public Dictionary<string, BoxScorePlayer> Players { get; set; } = null!;
        public List<long> Scratches { get; set; } = null!;
        public List<Penalty> PenaltyBox { get; set; } = null!;
        public List<Coach> Coaches { get; set; } = null!;
    }

    public class Penalty
    {
        public long Id { get; set; }
        public string TimeRemaining { get; set; } = null!;
        public bool Active { get; set; }
    } 
}