namespace DraftPuck.Application.Features.Lobbies;

public class MakePickCommand : IRequest<LobbyMemberPickDto>
{
    public int PlayerId { get; set; }
    public int GameId { get; set; }
    public int TeamId { get; set; }
    public Guid? LobbyMemberId { get; set; }
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
}