using DraftPuck.Shared.Enums;

namespace DraftPuck.Shared.Entities;

public partial class LobbyEvent
{
    public Guid Id { get; set; }
    public DateTime TimeUtc { get; set; }
    public string Title { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string? Subtext { get; set; }
    public int? PlayerId { get; set; }
    public int? Player2Id { get; set; }
    public int? TeamId { get; set; }
    public DateTime Created { get; set; }
    public int? GameEventId { get; set; }
    public int? GameId { get; set; }
    public bool IsSent { get; set; }
    public DateTime? LastSendAttempt { get; set; }
    public int SendAttempts { get; set; }
    public Guid? LobbyId { get; set; }
    public LobbyEventType LobbyEventType { get; set; }
    public Guid? LobbyMemberId { get; set; }
    public Guid? LobbyMember2Id { get; set; }
}
