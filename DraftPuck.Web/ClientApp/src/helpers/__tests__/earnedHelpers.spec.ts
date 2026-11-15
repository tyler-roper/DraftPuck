import { describe, expect, it } from 'vitest'
import { howEarned } from '@/helpers/earnedHelpers'

const achievement = (achievementId: string, dateEarned?: Date): UserAchievement => ({
  achievementId,
  uniqueIdentifier: achievementId,
  friendlyName: `Name of ${achievementId}`,
  description: `Description of ${achievementId}`,
  dateEarned
})

describe('howEarned', () => {
  it('names the achievement that earned it, and when', () => {
    const earned = new Date(2026, 10, 3)

    expect(howEarned({ achievementId: 'a2' }, [achievement('a1'), achievement('a2', earned)])).toEqual({
      kind: 'achievement',
      name: 'Name of a2',
      description: 'Description of a2',
      dateEarned: earned
    })
  })

  it('says a default is one every player has', () => {
    expect(howEarned({ achievementId: null }, [achievement('a1')])).toEqual({ kind: 'default' })
  })

  it("says nothing when the user doesn't have the achievement it came from", () => {
    expect(howEarned({ achievementId: 'a9' }, [achievement('a1')])).toBeNull()
  })
})
