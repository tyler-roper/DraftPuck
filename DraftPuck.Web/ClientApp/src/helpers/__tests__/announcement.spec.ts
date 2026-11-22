import { beforeEach, describe, expect, it } from 'vitest'
import { announcement, dismissAnnouncement, isAnnouncementVisible } from '@/helpers/announcement'

const before = new Date(announcement.showUntil.getTime() - 1000)

describe('announcement', () => {
  beforeEach(() => localStorage.clear())

  it('shows until dismissed, and stays dismissed', () => {
    expect(isAnnouncementVisible(localStorage, before)).toBe(true)

    dismissAnnouncement(localStorage)

    expect(isAnnouncementVisible(localStorage, before)).toBe(false)
  })

  it('shows again for a new announcement', () => {
    localStorage.setItem('draftpuck:announcementDismissed', 'an-older-one')

    expect(isAnnouncementVisible(localStorage, before)).toBe(true)
  })

  it('is gone for everyone once it expires', () => {
    expect(isAnnouncementVisible(localStorage, announcement.showUntil)).toBe(false)
  })

  it('still shows when storage is blocked', () => {
    const blocked = {
      getItem: () => {
        throw new Error('blocked')
      },
      setItem: () => {
        throw new Error('blocked')
      }
    } as unknown as Storage

    expect(() => dismissAnnouncement(blocked)).not.toThrow()
    expect(isAnnouncementVisible(blocked, before)).toBe(true)
  })
})
