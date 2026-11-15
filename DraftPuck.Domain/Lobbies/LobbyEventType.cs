namespace DraftPuck.Domain.Lobbies;

public enum LobbyEventType
{
    LobbyCreated,
    UserJoined,
    UserNameChanged,
    NewPick,
    DrinkAwarded,
    DrinkAssigned,
    DrinkInvalidated,
    DrinkRevoked,
    GoalChanged,
    GoalRemoved,
    UserRejoined,
    UserRemoved,
    Broadcast,
    PickRemoved,
    UserLeft,
    UserPromoted,
    // Stored as an int and mirrored in the client's lobbyEventType.ts: add new values at the end only.
    PickScratched,
    /// <summary>Bots picked for a game: their first picks once its lineups are out, or replacements for lost picks.</summary>
    BotPicksMade
}
