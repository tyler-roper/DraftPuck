namespace DraftPuck.Api.Models
{
    public class LobbyMemberPickResponse
    {
        public Guid Id { get; set; }
        public Guid LobbyMemberId { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public DateTime Created { get; set; }
        public List<DrinkResponse> Drinks { get; set; } = new();
    }
}