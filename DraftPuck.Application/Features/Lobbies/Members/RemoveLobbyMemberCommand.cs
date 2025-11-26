namespace DraftPuck.Application.Features.Lobbies.Members;

public class RemoveLobbyMemberCommand : IRequest
{
    public string Code { get; set; } = null!;
    public Guid LobbyMemberId { get; set; }
    public Guid RequesterUserId { get; set; }
}