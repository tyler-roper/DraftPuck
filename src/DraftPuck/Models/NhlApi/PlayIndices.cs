namespace DraftPuck.Models.NhlApi
{
public class PlayIndices
    {
        public int StartIndex { get; set; }
        public List<int> Plays { get; set; } = null!;
        public int EndIndex { get; set; }
    }
}