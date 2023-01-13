namespace DraftPuck.Models.NhlApi
{
    public class PlayerSeasonStats
    {
        public string TimeOnIce { get; set; } = null!;
        public int Assists { get; set; }
        public int Goals { get; set; }
        public int Pim { get; set; }
        public int Shots { get; set; }
        public int Games { get; set; }
        public int Hits { get; set; }
        public int PowerPlayGoals { get; set; }
        public int PowerPlayPoints { get; set; }
        public string PowerPlayTimeOnIce { get; set; } = null!;
        public string EvenTimeOnIce { get; set; } = null!;
        public int PenaltyMinutes { get; set; }
        public int FaceOffPct { get; set; }
        public int ShotPct { get; set; }
        public int GameWinningGoals { get; set; }
        public int OverTimeGoals { get; set; }
        public int ShortHandedGoals { get; set; }
        public int ShortHandedPoints { get; set; }
        public string ShortHandedTimeOnIce { get; set; } = null!;
        public int Blocked { get; set; }
        public int PlusMinus { get; set; }
        public int Points { get; set; }
        public int Shifts { get; set; }
        public string TimeOnIcePerGame { get; set; } = null!;
        public string EvenTimeOnIcePerGame { get; set; } = null!;
        public string ShortHandedTimeOnIcePerGame { get; set; } = null!;
        public string PowerPlayTimeOnIcePerGame { get; set; } = null!;
    }
}
