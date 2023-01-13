namespace DraftPuck.Models.NhlApi
{
public class Division
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string NameShort { get; set; } = null!;
        public string Link { get; set; } = null!;
        public string Abbreviation { get; set; } = null!;
    }
}