/** A live lobby at a glance, for the rejoin button: who hosts it ("you" for your own) and how big it is. */
export function rejoinDetails(
  lobby: Pick<LobbySummary, 'createdBy' | 'createdByName' | 'memberCount' | 'gameCount'>,
  userId: string | undefined
): { host: string; players: string; games: string } {
  return {
    host: lobby.createdBy === userId ? 'you' : lobby.createdByName,
    players: lobby.memberCount === 1 ? '1 player' : `${lobby.memberCount} players`,
    games: lobby.gameCount === 1 ? '1 game' : `${lobby.gameCount} games`
  }
}

/**
 * The key a dismissal is remembered under: the set of live lobbies. Dismissing hides the button for those lobbies;
 * joining or starting another brings it back.
 */
export function rejoinDismissKey(lobbies: Pick<LobbySummary, 'joinCode'>[]): string {
  return lobbies
    .map((l) => l.joinCode.toUpperCase())
    .sort()
    .join(',')
}

/**
 * The lobby to open straight away when the app opens on the home page, so a player whose phone closed the app mid-game
 * lands back in it: their one live lobby, unless they dismissed the rejoin bar for it. With several, they choose.
 */
export function lobbyToOpenOnLaunch<T extends Pick<LobbySummary, 'joinCode'>>(lobbies: T[], dismissedKey: string | null): T | undefined {
  if (lobbies.length !== 1) return undefined
  return dismissedKey === rejoinDismissKey(lobbies) ? undefined : lobbies[0]
}
