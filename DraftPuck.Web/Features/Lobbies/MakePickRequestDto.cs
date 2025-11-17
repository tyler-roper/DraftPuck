namespace DraftPuck.Web.Features.Lobbies;

public class MakePickRequestDto
{
    public int PlayerId { get; set; }
    public int GameId { get; set; }
    public int TeamId { get; set; }
    public Guid LobbyMemberId { get; set; }
}
