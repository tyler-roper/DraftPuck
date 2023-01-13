namespace DraftPuck.Models.NhlApi
{
public class GoalieStats
    {
        public string TimeOnIce { get; set; } = null!;
        public int Assists { get; set; }
        public int Goals { get; set; }
        public int Pim { get; set; }
        public int Shots { get; set; }
        public int Saves { get; set; }
        public int PowerPlaySaves { get; set; }
        public int ShortHandedSaves { get; set; }
        public int EvenSaves { get; set; }
        public int ShortHandedShotsAgainst { get; set; }
        public int EvenShotsAgainst { get; set; }
        public int PowerPlayShotsAgainst { get; set; }
        public string Decision { get; set; } = null!;
        public decimal SavePercentage { get; set; }
        public decimal PowerPlaySavePercentage { get; set; }
        public decimal ShortHandedSavePercentage { get; set; }
        public decimal EvenStrengthSavePercentage { get; set; }
    }
}