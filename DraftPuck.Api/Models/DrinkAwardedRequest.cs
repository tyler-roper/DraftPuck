namespace DraftPuck.Api.Models
{
    public class DrinkAwardedRequest
    {
        public Guid LobbyMemberPickId { get; set; }
        public int EventId { get; set; }
    }
}