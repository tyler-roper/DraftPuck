namespace BrewPuck.Models.NHL
{
    public class About
    {
        public int eventIdx { get; set; }
        public int eventId { get; set; }
        public int period { get; set; }
        public string periodType { get; set; }
        public string ordinalNum { get; set; }
        public string periodTime { get; set; }
        public string periodTimeRemaining { get; set; }
        public DateTime dateTime { get; set; }
        public Goals goals { get; set; }
    }

    public class Away
    {
        public LeagueRecord leagueRecord { get; set; }
        public int score { get; set; }
        public Team team { get; set; }
    }

    public class Content
    {
        public string link { get; set; }
    }

    public class Coordinates
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class Date
    {
        public string date { get; set; }
        public int totalItems { get; set; }
        public int totalEvents { get; set; }
        public int totalGames { get; set; }
        public int totalMatches { get; set; }
        public List<NhlGame> games { get; set; }
        public List<object> events { get; set; }
        public List<object> matches { get; set; }
    }

    public class NhlGame
    {
        public int gamePk { get; set; }
        public string link { get; set; }
        public string gameType { get; set; }
        public string season { get; set; }
        public DateTime gameDate { get; set; }
        public Status status { get; set; }
        public Teams teams { get; set; }
        public List<ScoringPlay> scoringPlays { get; set; }
        public Venue venue { get; set; }
        public Content content { get; set; }
    }

    public class Goals
    {
        public int away { get; set; }
        public int home { get; set; }
    }

    public class Home
    {
        public LeagueRecord leagueRecord { get; set; }
        public int score { get; set; }
        public Team team { get; set; }
    }

    public class LeagueRecord
    {
        public int wins { get; set; }
        public int losses { get; set; }
        public int ot { get; set; }
        public string type { get; set; }
    }

    public class MetaData
    {
        public string timeStamp { get; set; }
    }

    public class Player
    {
        public Player2 player { get; set; }
        public string playerType { get; set; }
        public int seasonTotal { get; set; }
    }

    public class Player2
    {
        public int id { get; set; }
        public string fullName { get; set; }
        public string link { get; set; }
    }

    public class Result
    {
        public string @event { get; set; }
        public string eventCode { get; set; }
        public string eventTypeId { get; set; }
        public string description { get; set; }
        public string secondaryType { get; set; }
        public Strength strength { get; set; }
        public bool gameWinningGoal { get; set; }
        public bool emptyNet { get; set; }
    }

    public class Schedule
    {
        public string copyright { get; set; }
        public int totalItems { get; set; }
        public int totalEvents { get; set; }
        public int totalGames { get; set; }
        public int totalMatches { get; set; }
        public MetaData metaData { get; set; }
        public int wait { get; set; }
        public List<Date> dates { get; set; }
    }

    public class ScoringPlay
    {
        public List<Player> players { get; set; }
        public Result result { get; set; }
        public About about { get; set; }
        public Coordinates coordinates { get; set; }
        public Team team { get; set; }
    }

    public class Status
    {
        public string abstractGameState { get; set; }
        public string codedGameState { get; set; }
        public string detailedState { get; set; }
        public string statusCode { get; set; }
        public bool startTimeTBD { get; set; }
    }

    public class Strength
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class Team
    {
        public int id { get; set; }
        public string name { get; set; }
        public string link { get; set; }
    }

    public class Teams
    {
        public Away away { get; set; }
        public Home home { get; set; }
    }

    public class Venue
    {
        public string name { get; set; }
        public string link { get; set; }
        public int? id { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Conference
    {
        public int id { get; set; }
        public string name { get; set; }
        public string link { get; set; }
    }

    public class Division
    {
        public int id { get; set; }
        public string name { get; set; }
        public string nameShort { get; set; }
        public string link { get; set; }
        public string abbreviation { get; set; }
    }

    public class Franchise
    {
        public int franchiseId { get; set; }
        public string teamName { get; set; }
        public string link { get; set; }
    }

    public class FullTeam
    {
        public int id { get; set; }
        public string name { get; set; }
        public string link { get; set; }
        public Venue venue { get; set; }
        public string abbreviation { get; set; }
        public string teamName { get; set; }
        public string locationName { get; set; }
        public string firstYearOfPlay { get; set; }
        public Division division { get; set; }
        public Conference conference { get; set; }
        public Franchise franchise { get; set; }
        public string shortName { get; set; }
        public string officialSiteUrl { get; set; }
        public int franchiseId { get; set; }
        public bool active { get; set; }
    }

    public class TimeZone
    {
        public string id { get; set; }
        public int offset { get; set; }
        public string tz { get; set; }
    }

    public class TeamsResponse
    {
        public string copyright { get; set; }
        public List<FullTeam> teams { get; set; }
    }
}
