namespace DraftPuck.Application.Features.Lobbies;

public class LobbyMemberPickDto
{
    public Guid Id { get; set; }
    public Guid LobbyMemberId { get; set; }
    public int PlayerId { get; set; }
    public int GameId { get; set; }
    public int TeamId { get; set; }
    public DateTime Created { get; set; }
    public bool IsActive { get; set; }
    public List<DrinkDto> Drinks { get; } = [];
}
