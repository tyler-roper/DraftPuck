namespace DraftPuck.NhlApi.Models
{
    public class Schedule
    {
        public string NextStartDate { get; set; } = null!;
        public string PreviousStartDate { get; set; } = null!;
        public List<GameDate> GameWeek { get; set; } = new();
    }
}
