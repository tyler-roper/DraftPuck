import { describe, expect, it } from 'vitest'
import { badgeText, friendStatusOf, inviteOptions, inviteSummary, pendingCount } from '@/helpers/friendHelpers'

const friend = (id: string, nickname = id): Friend => ({ id, nickname })
const invite = (joinCode: string): LobbyInvite => ({ joinCode, sender: friend('host'), gameCount: 2, memberCount: 3, created: new Date() })
const list = (overrides: Partial<FriendsList> = {}): FriendsList => ({ friends: [], incoming: [], outgoing: [], invites: [], ...overrides })

describe('friendStatusOf', () => {
  const friends = list({ friends: [friend('a')], incoming: [friend('b')], outgoing: [friend('c')] })

  it('tells friends, requests either way, and strangers apart', () => {
    expect(friendStatusOf(friends, 'a')).toBe('friends')
    expect(friendStatusOf(friends, 'b')).toBe('requestReceived')
    expect(friendStatusOf(friends, 'c')).toBe('requestSent')
    expect(friendStatusOf(friends, 'd')).toBe('none')
  })

  it('knows nothing before the list loads', () => {
    expect(friendStatusOf(undefined, 'a')).toBe('none')
  })
})

describe('pendingCount and badgeText', () => {
  it('counts requests to answer and invites, not requests the user sent', () => {
    expect(pendingCount(list({ incoming: [friend('a')], outgoing: [friend('b')], invites: [invite('ABCD'), invite('EFGH')] }))).toBe(3)
    expect(pendingCount(undefined)).toBe(0)
  })

  it('shows nothing at zero and caps at 9+', () => {
    expect(badgeText(0)).toBe('')
    expect(badgeText(3)).toBe('3')
    expect(badgeText(12)).toBe('9+')
  })
})

describe('inviteOptions', () => {
  it('marks members and invited friends, listing the ones still to invite first', () => {
    const options = inviteOptions([friend('m', 'Mo'), friend('z', 'zed'), friend('i', 'Ivy'), friend('a', 'Al')], ['m'], ['i'])

    expect(options.map((o) => [o.friend.nickname, o.state])).toEqual([
      ['Al', 'available'],
      ['zed', 'available'],
      ['Ivy', 'invited'],
      ['Mo', 'member']
    ])
  })
})

describe('inviteSummary', () => {
  it('describes the lobby in a few words', () => {
    expect(inviteSummary({ gameCount: 1, memberCount: 1 })).toBe('1 game · 1 player')
    expect(inviteSummary({ gameCount: 4, memberCount: 3 })).toBe('4 games · 3 players')
  })
})
