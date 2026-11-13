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
  PickScratched
}

export default LobbyEventType
