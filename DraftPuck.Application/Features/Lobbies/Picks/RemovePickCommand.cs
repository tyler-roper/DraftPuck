namespace DraftPuck.Application.Features.Lobbies.Picks;

public class RemovePickCommand : IRequest
{
    public string Code { get; set; } = null!;
    public Guid PickId { get; set; }
    public Guid UserId { get; set; }
}