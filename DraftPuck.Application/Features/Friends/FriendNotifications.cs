namespace DraftPuck.Application.Features.Friends;

public record FriendRequestedNotification(UserEntity Requester, UserEntity Addressee) : INotification;
public record FriendRequestAcceptedNotification(UserEntity Requester, UserEntity Addressee) : INotification;
/// <summary>A member invited some of their friends to their lobby.</summary>
public record LobbyInvitesSentNotification(LobbyEntity Lobby, LobbyMemberEntity Sender, IReadOnlyCollection<Guid> RecipientIds) : INotification;
