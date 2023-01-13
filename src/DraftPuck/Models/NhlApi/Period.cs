namespace DraftPuck.Models.NhlApi
{
public class Period
    {
        public string PeriodType { get; set; } = null!;
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;
        public int Num { get; set; }
        public string OrdinalNum { get; set; } = null!;
        public TeamPeriodStats Home { get; set; } = null!;
        public TeamPeriodStats Away { get; set; } = null!;
    }
}