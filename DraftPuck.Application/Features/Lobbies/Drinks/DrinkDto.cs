namespace DraftPuck.Application.Features.Lobbies.Drinks;

public class DrinkDto
{
    public Guid Id { get; set; }
    public Guid LobbyMemberPickId { get; set; }
    public Guid? RecipientLobbyMemberId { get; set; }
    public int EventId { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Assigned { get; set; }
}
