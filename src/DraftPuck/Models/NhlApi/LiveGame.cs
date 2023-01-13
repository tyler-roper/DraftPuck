namespace DraftPuck.Models.NhlApi
{
    public class LiveGame
    {
        public string Copyright { get; set; } = null!;
        public long GamePk { get; set; }
        public string Link { get; set; } = null!;
        public Metadata MetaData { get; set; } = null!;
        public GameData GameData { get; set; } = null!;
        public LiveData LiveData { get; set; } = null!;
    }
}
