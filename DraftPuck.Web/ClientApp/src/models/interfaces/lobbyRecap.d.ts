/** A lobby's end-of-night awards, from the server. */
interface LobbyRecap {
  awards: Array<LobbyAward>
  /** The players picked in the lobby, named. */
  players: Array<RecapPlayer>
}

interface RecapPlayer {
  gameId: number
  playerId: number
  /** "Cale Makar". */
  name: string
  /** The team's abbreviation, e.g. "COL". */
  team: string
}

interface LobbyAward {
  /** Which award, e.g. best_pick or rivalry (see LobbyAwards on the server). */
  key: string
  title: string
  memberId: string
  memberName: string
  /** For a rivalry, the other member. */
  otherMemberId?: string | null
  /** What they did, e.g. "McDavid (EDM) earned 4 drinks". */
  detail: string
}
