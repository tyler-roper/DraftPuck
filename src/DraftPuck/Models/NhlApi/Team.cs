namespace DraftPuck.Models.NhlApi
{
public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Link { get; set; } = null!;
        public Venue Venue { get; set; } = null!;
        public string Abbreviation { get; set; } = null!;
        public string TriCode { get; set; } = null!;
        public string TeamName { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public string FirstYearOfPlay { get; set; } = null!;
        public Division Division { get; set; } = null!;
        public Conference Conference { get; set; } = null!;
        public Franchise Franchise { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string OfficialSiteUrl { get; set; } = null!;
        public int FranchiseId { get; set; }
        public bool Active { get; set; }
    }
}