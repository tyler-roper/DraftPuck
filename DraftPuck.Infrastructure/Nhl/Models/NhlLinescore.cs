namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlLinescore
{
    public List<NhlLinescorePeriod> ByPeriod { get; set; } = [];
    public NhlLinescoreTotals Totals { get; set; } = null!;
}
