namespace DraftPuck.Application.Features.Lobbies.Picks;

public class RemovePickCommand : IRequest
{
    public string Code { get; set; } = null!;
    public Guid PickId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// Set when the system removes a pick (e.g. the player left the lineup), bypassing user permission checks.
    /// </summary>
    public bool IsSystemRemoval { get; set; }
}
