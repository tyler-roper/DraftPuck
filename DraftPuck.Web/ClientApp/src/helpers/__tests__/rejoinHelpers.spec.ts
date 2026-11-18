import { describe, expect, it } from 'vitest'
import { rejoinDetails, rejoinDismissKey } from '@/helpers/rejoinHelpers'

const lobby = (overrides: Partial<LobbySummary> = {}) => ({ createdBy: 'host', createdByName: 'Sam', memberCount: 5, gameCount: 3, ...overrides })

describe('rejoinDetails', () => {
  it('names the host, or "You" for your own lobby', () => {
    expect(rejoinDetails(lobby(), 'me')).toBe('Host: Sam · 5 players · 3 games')
    expect(rejoinDetails(lobby({ createdBy: 'me' }), 'me')).toBe('Host: You · 5 players · 3 games')
  })

  it('says 1 player and 1 game', () => {
    expect(rejoinDetails(lobby({ memberCount: 1, gameCount: 1 }), 'me')).toBe('Host: Sam · 1 player · 1 game')
  })
})

describe('rejoinDismissKey', () => {
  it('is the same set of lobbies whatever the order or case', () => {
    expect(rejoinDismissKey([{ joinCode: 'efgh' }, { joinCode: 'ABCD' }])).toBe(rejoinDismissKey([{ joinCode: 'ABCD' }, { joinCode: 'EFGH' }]))
  })

  it('changes when another lobby is added', () => {
    expect(rejoinDismissKey([{ joinCode: 'ABCD' }])).not.toBe(rejoinDismissKey([{ joinCode: 'ABCD' }, { joinCode: 'EFGH' }]))
  })
})
