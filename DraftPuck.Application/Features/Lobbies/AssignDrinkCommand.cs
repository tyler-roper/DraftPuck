namespace DraftPuck.Application.Features.Lobbies;
public class AssignDrinkCommand : IRequest<DrinkDto>
{
    public string Code { get; set; } = null!;
    public Guid RecipientLobbyMemberId { get; set; }
    public Guid DrinkId { get; set; }
    public Guid AssignerUserId { get; set; }
}