namespace DraftPuck.NhlApi.Models
{
    public class Situation
    {
        public TeamSummary HomeTeam { get; set; }
        public TeamSummary AwayTeam { get; set; }
        public string SituationCode { get; set; }
        public string TimeRemaining { get; set; }
        public int SecondsRemaining { get; set; }
    }


}
