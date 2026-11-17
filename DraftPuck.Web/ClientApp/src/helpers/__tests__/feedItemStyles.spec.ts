import { describe, expect, it } from 'vitest'
import LobbyEventType from '@/enums/lobbyEventType'
import { drinkAvatarMemberId, lobbyEventStyle } from '@/helpers/feedItemStyles'

describe('lobbyEventStyle', () => {
  it('colours lobby events by kind', () => {
    expect(lobbyEventStyle(LobbyEventType.DrinkAwarded).tone).toBe('drink')
    expect(lobbyEventStyle(LobbyEventType.NewPick).tone).toBe('pick')
    expect(lobbyEventStyle(LobbyEventType.UserJoined).tone).toBe('social')
    expect(lobbyEventStyle(LobbyEventType.GoalChanged).tone).toBe('correction')
  })

  it('gives callouts their own colour and icon', () => {
    expect(lobbyEventStyle(LobbyEventType.OnFire)).toMatchObject({ tone: 'callout', icon: 'fire-flame-curved', color: '#ff6a00' })
  })

  it('shows the earner on a drink awarded and the drinker on a drink assigned', () => {
    const event = { lobbyMemberId: 'tyler', lobbyMember2Id: 'casey' } as LobbyEvent

    expect(drinkAvatarMemberId(event, lobbyEventStyle(LobbyEventType.DrinkAwarded).avatar!)).toBe('tyler')
    expect(drinkAvatarMemberId(event, lobbyEventStyle(LobbyEventType.DrinkAssigned).avatar!)).toBe('casey')
  })

  it("draws event types it doesn't know as plain events", () => {
    expect(lobbyEventStyle(999 as LobbyEventType)).toEqual({ tone: 'social', icon: 'info' })
  })
})
