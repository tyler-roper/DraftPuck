namespace DraftPuck.Models
{
public class LobbyMemberPickResponse
    {
        public Guid Id { get; set; }
        public Guid LobbyMemberId { get; set; }
        public long PlayerId { get; set; }
        public long GamePk { get; set; }
        public DateTime Created { get; set; }
        public List<DrinkResponse> Drinks { get; set; } = new();
    }
}