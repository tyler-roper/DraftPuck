namespace DraftPuck.Application.Features.Lobbies.Drinks;
public class AssignDrinkCommand : IRequest<DrinkDto>
{
    public string Code { get; set; } = null!;
    public Guid RecipientLobbyMemberId { get; set; }
    public Guid DrinkId { get; set; }
    public Guid AssignerUserId { get; set; }
    /// <summary>An optional message to go with the drink (<see cref="DrinkMessages"/>).</summary>
    public string? Message { get; set; }
}