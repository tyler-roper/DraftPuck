namespace DraftPuck.Api.Models
{
    public class LobbyMemberResponse
    {
        public Guid Id { get; set; }
        public Guid LobbyId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Joined { get; set; }
        public string Name { get; set; } = null!;
        public bool IsBot { get; set; }
        public BotPickStyle BotPickStyle { get; set; }
        public List<LobbyMemberPickResponse> Picks { get; set; } = new();
        public List<MessageModel> Messages { get; set; } = new();
    }
}