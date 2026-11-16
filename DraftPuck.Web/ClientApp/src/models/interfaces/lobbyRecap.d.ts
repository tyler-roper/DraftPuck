/** A lobby's end-of-night awards, from the server. */
interface LobbyRecap {
  awards: Array<LobbyAward>
}

interface LobbyAward {
  /** Which award, e.g. best_pick or rivalry (see LobbyAwards on the server). */
  key: string
  title: string
  memberId: string
  memberName: string
  /** What they did, e.g. "McDavid (EDM) earned 4 drinks". */
  detail: string
}
