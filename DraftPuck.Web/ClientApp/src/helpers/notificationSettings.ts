import NotificationPreference from '@/enums/notificationPreference'

/** The settings that can differ per lobby. */
export type LobbyNotificationKey =
  | 'drinkAwardedNotificationPreference'
  | 'drinkReceivedNotificationPreference'
  | 'chatNotificationPreference'
  | 'pickingStartedNotificationPreference'

/** Every setting a user has defaults for: the lobby ones, plus achievements (which aren't tied to a lobby). */
export type NotificationKey = LobbyNotificationKey | 'achievementAwardedNotificationPreference'

export type LobbyNotificationPreferences = Record<LobbyNotificationKey, NotificationPreference>
export type NotificationPreferences = Record<NotificationKey, NotificationPreference>

export interface NotificationSetting {
  key: NotificationKey
  question: string
  options: { value: NotificationPreference; label: string }[]
}

const off = { value: NotificationPreference.None, label: 'Do not notify me.' }
const always = { value: NotificationPreference.All, label: 'Always notify me.' }

/** The questions and choices shown for each setting, in display order. */
export const notificationSettings: NotificationSetting[] = [
  {
    key: 'drinkAwardedNotificationPreference',
    question: '🚨 When a drink is awarded...',
    options: [off, { value: NotificationPreference.Relevant, label: "Notify me if it's mine." }, always]
  },
  {
    key: 'drinkReceivedNotificationPreference',
    question: '🍺 When a drink is assigned...',
    options: [off, { value: NotificationPreference.Relevant, label: "Notify me if it's assigned to me." }, always]
  },
  {
    key: 'chatNotificationPreference',
    question: "💬 When there's a new message in chat...",
    options: [off, { value: NotificationPreference.Relevant, label: "Notify me if I'm mentioned." }, always]
  },
  {
    key: 'pickingStartedNotificationPreference',
    question: '📌 When picking becomes available for a new game...',
    options: [off, always]
  },
  {
    key: 'achievementAwardedNotificationPreference',
    question: '🏆 When I earn a new achievement...',
    options: [off, always]
  }
]

export const lobbyNotificationKeys: LobbyNotificationKey[] = [
  'drinkAwardedNotificationPreference',
  'drinkReceivedNotificationPreference',
  'chatNotificationPreference',
  'pickingStartedNotificationPreference'
]

export const allNotificationKeys: NotificationKey[] = [...lobbyNotificationKeys, 'achievementAwardedNotificationPreference']

/** Just the given settings, copied out of something that has them (a user, or a lobby's settings). */
export function pickPreferences<K extends NotificationKey>(source: Partial<Record<K, NotificationPreference>>, keys: K[]): Record<K, NotificationPreference> {
  return Object.fromEntries(keys.map((key) => [key, source[key] ?? NotificationPreference.None])) as Record<K, NotificationPreference>
}

export function hasPreferenceChanges<K extends NotificationKey>(
  current: Record<K, NotificationPreference>,
  saved: Record<K, NotificationPreference>,
  keys: K[]
): boolean {
  return keys.some((key) => current[key] !== saved[key])
}
