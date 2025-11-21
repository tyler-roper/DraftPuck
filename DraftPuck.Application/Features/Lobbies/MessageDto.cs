namespace DraftPuck.Application.Features.Lobbies;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid LobbyMemberId { get; set; }
    public string Message { get; set; } = null!;
    public DateTime Sent { get; set; }
}