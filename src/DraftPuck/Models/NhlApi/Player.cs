namespace DraftPuck.Models.NhlApi
{
    public class Player
    {
        public long Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Link { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PrimaryNumber { get; set; } = null!;
        public string BirthDate { get; set; } = null!;
        public int CurrentAge { get; set; }
        public string BirthCity { get; set; } = null!;
        public string BirthStateProvince { get; set; } = null!;
        public string BirthCountry { get; set; } = null!;
        public string Nationality { get; set; } = null!;
        public string Height { get; set; } = null!;
        public int Weight { get; set; }
        public bool Active { get; set; }
        public bool AlternateCaptain { get; set; }
        public bool Captain { get; set; }
        public bool Rookie { get; set; }
        public string ShootsCatches { get; set; } = null!;
        public string RosterStatus { get; set; } = null!;
        public TeamSummary CurrentTeam = null!;
        public Position PrimaryPosition { get; set; } = null!;
    }
}

