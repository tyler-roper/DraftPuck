enum LobbyEventType {
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
  // Mirrors the server's LobbyEventType ints: add new values at the end only.
  PickScratched,
  BotPicksMade,
  // Callouts: notable moments during a lobby.
  OnFire,
  DrinkingStreak,
  RivalryBrewing,
  IceCold,
  /** The host merged a duplicate spot (lobbyMember2Id, removed) into the original one (lobbyMemberId). */
  MembersMerged
}

export default LobbyEventType
