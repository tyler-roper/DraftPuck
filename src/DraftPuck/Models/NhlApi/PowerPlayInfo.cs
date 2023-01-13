namespace DraftPuck.Models.NhlApi
{
    public class PowerPlayInfo
    {
        public int SituationTimeRemaining { get; set; }
        public int SituationTimeElapsed { get; set; }
        public bool InSituation { get; set; }
    }
}