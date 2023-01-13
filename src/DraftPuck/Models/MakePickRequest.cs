namespace DraftPuck.Models
{
public class MakePickRequest
    {
        public Guid? LobbyMemberId { get; set; }
        public long GamePk { get; set; }
        public long PlayerId { get; set; }
        public int TeamId { get; set; }
    }
}