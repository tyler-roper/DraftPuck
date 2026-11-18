using DraftPuck.Domain.Lobbies;
using DraftPuck.Domain.Users;

namespace DraftPuck.Domain.Friends;

/// <summary>
/// A player inviting a friend to a lobby they're in. It waits in the friend's invites until they join the lobby,
/// dismiss it, or the lobby ends.
/// </summary>
public class LobbyInviteEntity
{
    public Guid Id { get; set; }

    public Guid LobbyId { get; set; }

    public Guid SenderId { get; set; }

    public Guid RecipientId { get; set; }

    public DateTime Created { get; set; }

    public virtual LobbyEntity Lobby { get; set; } = null!;

    public virtual UserEntity Sender { get; set; } = null!;

    public virtual UserEntity Recipient { get; set; } = null!;
}
