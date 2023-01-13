namespace DraftPuck.Models.NhlApi
{
public class StatGameType
    {
        public string id { get; set; } = null!;
        public string description { get; set; } = null!;
        public bool Postseason { get; set; }
}
}