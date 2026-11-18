/** Where the current user stands with another player. */
export type FriendStatus = 'none' | 'requestSent' | 'requestReceived' | 'friends'

export function friendStatusOf(list: FriendsList | undefined, userId: string): FriendStatus {
  if (!list) return 'none'
  const has = (friends: Friend[]) => friends.some((f) => f.id === userId)
  if (has(list.friends)) return 'friends'
  if (has(list.incoming)) return 'requestReceived'
  if (has(list.outgoing)) return 'requestSent'
  return 'none'
}

/** What's waiting on the user: friend requests to answer and lobby invites to join or dismiss. */
export function pendingCount(list: FriendsList | undefined): number {
  return list ? list.incoming.length + list.invites.length : 0
}

/** A count for a badge: nothing at zero, and "9+" past nine so it stays small. */
export function badgeText(count: number): string {
  if (count <= 0) return ''
  return count > 9 ? '9+' : String(count)
}

export type InviteOptionState = 'available' | 'invited' | 'member'

export interface InviteOption {
  friend: Friend
  state: InviteOptionState
}

/**
 * The friends to offer in a lobby's invite list: everyone, marked if they're already in it or already invited, with
 * the ones who can still be invited first (each group alphabetical).
 */
export function inviteOptions(friends: Friend[], memberUserIds: string[], invitedUserIds: string[]): InviteOption[] {
  const members = new Set(memberUserIds)
  const invited = new Set(invitedUserIds)
  const order: Record<InviteOptionState, number> = { available: 0, invited: 1, member: 2 }
  return friends
    .map((friend) => ({ friend, state: (members.has(friend.id) ? 'member' : invited.has(friend.id) ? 'invited' : 'available') as InviteOptionState }))
    .sort((a, b) => order[a.state] - order[b.state] || a.friend.nickname.localeCompare(b.friend.nickname, undefined, { sensitivity: 'base' }))
}

/** "2 games · 3 players": what an invite's lobby looks like. */
export function inviteSummary(invite: Pick<LobbyInvite, 'gameCount' | 'memberCount'>): string {
  const games = invite.gameCount === 1 ? '1 game' : `${invite.gameCount} games`
  const players = invite.memberCount === 1 ? '1 player' : `${invite.memberCount} players`
  return `${games} · ${players}`
}
