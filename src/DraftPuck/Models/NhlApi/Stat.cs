namespace DraftPuck.Models.NhlApi
{
public class Stat
    {
        public StatType Type { get; set; } = null!;
        public List<StatSplit> Splits { get; set; } = null!;
    }
}