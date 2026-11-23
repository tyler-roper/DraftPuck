/**
 * Merging a duplicate spot into an original one: a guest lost their session (closed an incognito tab, switched
 * phones) and joined again under a new name. The host picks the duplicate, then the spot it really is. The original
 * spot stays and moves to the duplicate's login; the duplicate is removed, with anything it did. The server checks all
 * of this too.
 */

/** The spots a duplicate could really be: other guests still in the lobby. Not the host's spot, bots or accounts. */
export function mergeCandidates(lobby: Lobby, duplicate: LobbyMember): LobbyMember[] {
  if (duplicate.isBot || duplicate.isRemoved) return []
  return lobby.members.filter((m) => m.id !== duplicate.id && !m.isBot && !m.isRemoved && m.isGuest !== false && m.userId !== lobby.createdBy)
}

export interface SpotContents {
  picks: number
  drinksEarned: number
  drinksTaken: number
}

/** What a spot holds: its picks, the drinks they earned, and the drinks it was given. */
export function spotContents(lobby: Lobby, member: LobbyMember): SpotContents {
  return {
    picks: member.picks.length,
    drinksEarned: member.picks.reduce((count, pick) => count + pick.drinks.length, 0),
    drinksTaken: lobby.members.flatMap((m) => m.picks.flatMap((p) => p.drinks)).filter((d) => d.recipientLobbyMemberId === member.id).length
  }
}

function plural(count: number, word: string): string {
  return `${count} ${word}${count === 1 ? '' : 's'}`
}

/** "2 picks, 3 drinks earned and 1 drink taken", leaving out what's zero; "nothing yet" for an empty spot. */
export function describeSpotContents(contents: SpotContents): string {
  const parts = [
    contents.picks > 0 ? plural(contents.picks, 'pick') : undefined,
    contents.drinksEarned > 0 ? `${plural(contents.drinksEarned, 'drink')} earned` : undefined,
    contents.drinksTaken > 0 ? `${plural(contents.drinksTaken, 'drink')} taken` : undefined
  ].filter((part): part is string => !!part)

  if (parts.length === 0) return 'nothing yet'
  if (parts.length === 1) return parts[0]
  return `${parts.slice(0, -1).join(', ')} and ${parts[parts.length - 1]}`
}
