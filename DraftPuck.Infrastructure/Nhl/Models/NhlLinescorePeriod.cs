namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlLinescorePeriod
{
    public NhlPeriodDescriptor PeriodDescriptor { get; set; } = null!;
    public int Away { get; set; }
    public int Home { get; set; }
}
