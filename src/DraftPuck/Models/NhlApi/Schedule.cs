namespace DraftPuck.Models.NhlApi
{
    public class Schedule
    {
        public int TotalItems { get; set; }
        public int TotalEvents { get; set; }
        public int TotalGames { get; set; }
        public int TotalMatches { get; set; }
        public Metadata Metadata { get; set; } = null!;
        public int Wait { get; set; }
        public List<ScheduleDate> Dates { get; set; } = null!;
    }
}