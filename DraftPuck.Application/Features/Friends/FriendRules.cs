namespace DraftPuck.Application.Features.Friends;

public static class FriendRules
{
    /// <summary>Friends are for players with an account: a guest's identity lives on one device.</summary>
    public static bool CanHaveFriends(UserEntity user) => !user.IsGuest && !user.IsBot && user.IsActive;
}
