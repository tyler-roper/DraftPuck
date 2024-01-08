namespace Draftpuck.Nhl.Models
{
    public class NhlSchedule
    {
        public string NextStartDate { get; set; } = null!;
        public string PreviousStartDate { get; set; } = null!;
        public List<NhlGameDate> GameWeek { get; set; } = new();
    }
}
