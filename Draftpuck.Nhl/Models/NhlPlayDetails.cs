namespace Draftpuck.Nhl.Models
{
    public class NhlPlayDetails
    {
        public int EventOwnerTeamId { get; set; }
        public int LosingPlayerId { get; set; }
        public int WinningPlayerId { get; set; }
        public int XCoord { get; set; }
        public int YCoord { get; set; }
        public string ZoneCode { get; set; } = null!;
        public string TypeCode { get; set; } = null!;
        public string DescKey { get; set; } = null!;
        public int? Duration { get; set; }
        public int? CommittedByPlayerId { get; set; }
        public int? DrawnByPlayerId { get; set; }
        public string ShotType { get; set; } = null!;
        public int? ShootingPlayerId { get; set; }
        public int? GoalieInNetId { get; set; }
        public int? AwaySOG { get; set; }
        public int? HomeSOG { get; set; }
        public int? ScoringPlayerId { get; set; }
        public int? BlockingPlayerId { get; set; }
        public int? HittingPlayerId { get; set; }
        public int? HitteePlayerId { get; set; }
        public int? PlayerId { get; set; }
        public string Reason { get; set; } = null!;
        public string SecondaryReason { get; set; } = null!;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }


}
