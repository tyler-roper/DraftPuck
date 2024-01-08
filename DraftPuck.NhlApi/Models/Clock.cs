namespace DraftPuck.NhlApi.Models
{
    public class Clock
    {
        public string TimeRemaining { get; set; }
        public int SecondsRemaining { get; set; }
        public bool Running { get; set; }
        public bool InIntermission { get; set; }
    }


}
