namespace Draftpuck.Nhl.Models
{
    public class NhlLinescorePeriod
    {
        public int Period { get; set; }
        public NhlPeriodDescriptor PeriodDescriptor { get; set; } = null!;
        public int Away { get; set; }
        public int Home { get; set; }
    }
}
