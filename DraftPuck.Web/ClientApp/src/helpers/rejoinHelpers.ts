/** "Host: You · 5 players · 3 games": a live lobby, at a glance, for the rejoin button. */
export function rejoinDetails(lobby: Pick<LobbySummary, 'createdBy' | 'createdByName' | 'memberCount' | 'gameCount'>, userId: string | undefined): string {
  const host = lobby.createdBy === userId ? 'You' : lobby.createdByName
  const players = lobby.memberCount === 1 ? '1 player' : `${lobby.memberCount} players`
  const games = lobby.gameCount === 1 ? '1 game' : `${lobby.gameCount} games`
  return `Host: ${host} · ${players} · ${games}`
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
