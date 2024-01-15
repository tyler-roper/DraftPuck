namespace Draftpuck.NhlApi.Models;

public class NhlLinescore
{
    public List<NhlLinescorePeriod> ByPeriod { get; set; } = new();
    public NhlLinescoreTotals Totals { get; set; } = null!;
}
