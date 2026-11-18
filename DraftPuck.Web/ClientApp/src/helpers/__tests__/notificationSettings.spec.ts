import { describe, expect, it } from 'vitest'
import NotificationPreference from '@/enums/notificationPreference'
import {
  allNotificationKeys,
  hasPreferenceChanges,
  lobbyNotificationKeys,
  notificationSettings,
  pickPreferences
} from '@/helpers/notificationSettings'

describe('notificationSettings', () => {
  it('describes every setting once, in the order of allNotificationKeys', () => {
    expect(notificationSettings.map((s) => s.key)).toEqual(allNotificationKeys)
  })

  it('keeps achievements, the recap and friends out of the per-lobby settings', () => {
    expect(lobbyNotificationKeys).not.toContain('achievementAwardedNotificationPreference')
    expect(lobbyNotificationKeys).not.toContain('recapNotificationPreference')
    expect(lobbyNotificationKeys).not.toContain('friendNotificationPreference')
    expect(lobbyNotificationKeys).toHaveLength(4)
  })

  it('offers "only if it is about me" only where the server has that case', () => {
    const relevant = notificationSettings.filter((s) => s.options.some((o) => o.value === NotificationPreference.Relevant)).map((s) => s.key)
    expect(relevant).toEqual(['drinkAwardedNotificationPreference', 'drinkReceivedNotificationPreference', 'chatNotificationPreference'])
  })
})

describe('pickPreferences', () => {
  it('copies only the requested settings, treating missing ones as off', () => {
    const user = { chatNotificationPreference: NotificationPreference.All, nickname: 'Sam' }

    expect(pickPreferences(user, ['chatNotificationPreference', 'pickingStartedNotificationPreference'])).toEqual({
      chatNotificationPreference: NotificationPreference.All,
      pickingStartedNotificationPreference: NotificationPreference.None
    })
  })
})

describe('hasPreferenceChanges', () => {
  const saved = pickPreferences({}, lobbyNotificationKeys)

  it('is false when nothing changed', () => {
    expect(hasPreferenceChanges({ ...saved }, saved, lobbyNotificationKeys)).toBe(false)
  })

  it('is true when any setting changed', () => {
    expect(hasPreferenceChanges({ ...saved, chatNotificationPreference: NotificationPreference.All }, saved, lobbyNotificationKeys)).toBe(true)
  })
})
