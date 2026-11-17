namespace DraftPuck.Web.Features.Lobbies;
public class AssignDrinkRequestDto
{
    public Guid RecipientLobbyMemberId { get; set; }
    public string? Message { get; set; }
}