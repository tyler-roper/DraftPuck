namespace DraftPuck.Application.Features.Lobbies.Picks;

public class RemovePickCommand : IRequest
{
    public string Code { get; set; } = null!;
    public Guid PickId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// Set when the system removes a pick because its player left the lineup. Bypasses user permission checks, and the
    /// lobby and the pick's owner are told the player was scratched (<see cref="PickScratchedNotification"/>).
    /// </summary>
    public bool IsSystemRemoval { get; set; }
}
