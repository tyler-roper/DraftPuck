using DraftPuck.Shared.Users;

namespace DraftPuck.Shared.Lobbies;

public partial class LobbyMemberEntity
{
    public Guid Id { get; set; }

    public Guid LobbyId { get; set; }

    public Guid UserId { get; set; }

    public DateTime Joined { get; set; }

    public string Name { get; set; } = null!;

    public bool IsBot { get; set; } = false;

    public bool IsRemoved { get; set; } = false;

    public BotPickStyle? BotPickStyle { get; set; }

    // This member's push notification settings for this lobby. Copied from the user's defaults when they join
    // (see MemberNotificationPreferences), then changed per lobby. Achievements aren't per lobby, so they stay on the user.
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference ChatNotificationPreference { get; set; }
    public NotificationPreference PickingStartedNotificationPreference { get; set; }

    public virtual ICollection<DrinkEntity> Drinks { get; } = [];

    public virtual LobbyEntity Lobby { get; set; } = null!;

    public virtual ICollection<LobbyMemberPickEntity> LobbyMemberPicks { get; } = [];

    public virtual UserEntity User { get; set; } = null!;

    public ICollection<MessageEntity> Messages = [];
}
