namespace DraftPuck.Api.Models
{
    public class MessageModel
    {
        public Guid Id { get; set; }
        public Guid LobbyMemberId { get; set; }
        public string Message { get; set; } = null!;
        public DateTime Sent { get; set; }
    }
}
