namespace DraftPuck.Models.NhlApi
{
public class Franchise
    {
        public int FranchiseId { get; set; }
        public string TeamName { get; set; } = null!;
        public string Link { get; set; } = null!;
    }
}