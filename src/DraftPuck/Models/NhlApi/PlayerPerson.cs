namespace DraftPuck.Models.NhlApi
{
public class PlayerPerson
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Link { get; set; } = null!;
        public string ShootsCatches { get; set; } = null!;
        public string RosterStatus { get; set; } = null!;
    }
}