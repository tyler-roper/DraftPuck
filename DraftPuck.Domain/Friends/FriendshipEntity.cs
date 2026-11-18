using DraftPuck.Domain.Users;

namespace DraftPuck.Domain.Friends;

/// <summary>
/// Two users who are friends, or one asking the other to be. One row per pair, whichever of them asked first.
/// </summary>
public class FriendshipEntity
{
    public Guid Id { get; set; }

    public Guid RequesterId { get; set; }

    public Guid AddresseeId { get; set; }

    public DateTime Created { get; set; }

    /// <summary>When the addressee accepted; null while the request is pending.</summary>
    public DateTime? Accepted { get; set; }

    public bool IsAccepted => Accepted != null;

    public virtual UserEntity Requester { get; set; } = null!;

    public virtual UserEntity Addressee { get; set; } = null!;
}
