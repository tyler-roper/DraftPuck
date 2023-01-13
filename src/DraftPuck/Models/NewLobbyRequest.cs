namespace DraftPuck.Models
{
    public class NewLobbyRequest
    {
        public string Name { get; set; } = null!;
        public int PicksPerTeam { get; set; }
    }
}