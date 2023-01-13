namespace DraftPuck.Models.NhlApi
{
public class PlayAbout
    {
        public int EventIdx { get; set; }
        public int EventId { get; set; }
        public int Period { get; set; }
        public string PeriodType { get; set; } = null!;
        public string OrdinalNum { get; set; } = null!;
        public string PeriodTime { get; set; } = null!;
        public string PeriodTimeRemaining { get; set; } = null!;
        public DateTime DateTime { get; set; }
        public PlayAboutGoals Goals { get; set; } = null!;
    }
}