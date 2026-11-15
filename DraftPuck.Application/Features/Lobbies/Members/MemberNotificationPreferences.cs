using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Lobbies.Members;

/// <summary>
/// Notification settings live on each lobby member. A user's own settings are their defaults: copied ("stamped") onto
/// a member when they join a lobby, so changing your defaults later affects new lobbies, not ones you're already in
/// (unless you ask for that, see UpdateNotificationPreferencesCommand).
/// </summary>
public static class MemberNotificationPreferences
{
    public static void StampDefaults(LobbyMemberEntity member, UserEntity user)
    {
        member.DrinkReceivedNotificationPreference = user.DrinkReceivedNotificationPreference;
        member.DrinkAwardedNotificationPreference = user.DrinkAwardedNotificationPreference;
        member.ChatNotificationPreference = user.ChatNotificationPreference;
        member.PickingStartedNotificationPreference = user.PickingStartedNotificationPreference;
    }

    /// <summary>The API takes these as numbers; reject anything that isn't a known setting.</summary>
    public static void Validate(params NotificationPreference[] preferences)
    {
        if (preferences.Any(p => !Enum.IsDefined(p)))
            throw new ValidationException("Invalid notification setting.");
    }
}
