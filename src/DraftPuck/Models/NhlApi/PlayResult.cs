namespace DraftPuck.Models.NhlApi
{
    public class PlayResult
    {
        public string Event { get; set; } = null!;
        public string EventCode { get; set; } = null!;
        public string EventTypeId { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? SecondaryType { get; set; }
        public bool? EmptyNet { get; set; }
        public Strength? Strength { get; set; }
    }

    public class Strength
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}