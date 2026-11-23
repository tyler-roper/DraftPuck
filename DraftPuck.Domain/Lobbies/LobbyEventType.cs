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
    BotPicksMade,
    // Callouts: notable moments during a lobby (LobbyCallouts).
    /// <summary>A member's picks earned several drinks in a short time.</summary>
    OnFire,
    /// <summary>A member was handed several drinks in a short time.</summary>
    DrinkingStreak,
    /// <summary>Two members keep giving each other drinks.</summary>
    RivalryBrewing,
    /// <summary>Late in a game, none of a member's picks in it have scored.</summary>
    IceCold,
    /// <summary>
    /// The host merged a duplicate spot into the original one: same person, new login. LobbyMemberId is the original
    /// (kept) spot, LobbyMember2Id the removed duplicate.
    /// </summary>
    MembersMerged
}
