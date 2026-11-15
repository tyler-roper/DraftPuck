namespace DraftPuck.Application.Features.Achievements;

public enum AchievementTriggerType
{
    UserRegistration,
    LobbyCreated,
    LobbyJoined,
    DrinkAssigned,
    DrinkAwarded,
    PickMade,
    GoalScored,
    DiscordServerJoined,
    // Queue messages carry these as numbers: add new values at the end only.
    /// <summary>Someone joined a lobby the user hosts (checked for the host).</summary>
    HostedLobbyJoined,
    /// <summary>The user's name changed in a lobby.</summary>
    NameChanged,
    /// <summary>A lobby the user was in ended (deactivated at the daily cutoff).</summary>
    LobbyEnded,
    /// <summary>A drink the user gave or received was invalidated by a scoring change.</summary>
    DrinkInvalidated,
    /// <summary>A game the user had a pick in went final.</summary>
    GameFinished,
    /// <summary>One of the user's picks was scratched from the lineup.</summary>
    PickScratched,
    /// <summary>The user sent a chat message.</summary>
    MessageSent
}