namespace DraftPuck.Shared.Lobbies;

public partial class MessageEntity
{
    public Guid Id { get; set; }
    public Guid LobbyMemberId { get; set; }
    public string Message { get; set; } = null!;
    public DateTime Sent { get; set; }
    public bool IsDeleted { get; set; } = false;

    public LobbyMemberEntity LobbyMember { get; set; } = null!;
}
