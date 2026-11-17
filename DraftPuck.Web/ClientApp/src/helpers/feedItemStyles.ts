import LobbyEventType from '@/enums/lobbyEventType'
import { bannerStyle, isCallout } from '@/helpers/calloutHelpers'

/** How a lobby event looks in the feed: drinks amber, picks blue, comings and goings grey, corrections red, callouts their own colour. */
export type LobbyEventTone = 'drink' | 'pick' | 'social' | 'correction' | 'callout'

export interface LobbyEventStyle {
  tone: LobbyEventTone
  /** A Flaticon solid-rounded icon. */
  icon: string
  /** Drinks show a person instead of an icon: who earned it, or who's drinking it. */
  avatar?: 'earner' | 'recipient'
  /** A callout's colour. */
  color?: string
}

const styles: Partial<Record<LobbyEventType, LobbyEventStyle>> = {
  [LobbyEventType.LobbyCreated]: { tone: 'social', icon: 'flag-checkered' },
  [LobbyEventType.UserJoined]: { tone: 'social', icon: 'user-add' },
  [LobbyEventType.UserRejoined]: { tone: 'social', icon: 'user-add' },
  [LobbyEventType.UserLeft]: { tone: 'social', icon: 'exit' },
  [LobbyEventType.UserRemoved]: { tone: 'social', icon: 'remove-user' },
  [LobbyEventType.UserPromoted]: { tone: 'social', icon: 'crown' },
  [LobbyEventType.UserNameChanged]: { tone: 'social', icon: 'id-badge' },
  [LobbyEventType.Broadcast]: { tone: 'social', icon: 'megaphone' },
  [LobbyEventType.NewPick]: { tone: 'pick', icon: 'badge-check' },
  [LobbyEventType.BotPicksMade]: { tone: 'pick', icon: 'user-robot' },
  [LobbyEventType.PickRemoved]: { tone: 'pick', icon: 'undo' },
  [LobbyEventType.PickScratched]: { tone: 'pick', icon: 'user-slash' },
  [LobbyEventType.DrinkAwarded]: { tone: 'drink', icon: 'beer', avatar: 'earner' },
  [LobbyEventType.DrinkAssigned]: { tone: 'drink', icon: 'beer', avatar: 'recipient' },
  [LobbyEventType.DrinkInvalidated]: { tone: 'correction', icon: 'trash' },
  [LobbyEventType.DrinkRevoked]: { tone: 'correction', icon: 'comment-slash' },
  [LobbyEventType.GoalChanged]: { tone: 'correction', icon: 'shuffle' },
  [LobbyEventType.GoalRemoved]: { tone: 'correction', icon: 'cross-circle' }
}

/** How a lobby event is drawn in the feed. Unknown types (from a newer server) look like comings and goings. */
export function lobbyEventStyle(type: LobbyEventType): LobbyEventStyle {
  if (isCallout(type)) {
    const callout = bannerStyle(type)!
    return { tone: 'callout', icon: callout.icon, color: callout.color }
  }
  return styles[type] ?? { tone: 'social', icon: 'info' }
}

/** Whose avatar a drink event shows: the member who earned it, or the one drinking it. */
export function drinkAvatarMemberId(event: LobbyEvent, avatar: 'earner' | 'recipient'): string | undefined {
  return (avatar === 'earner' ? event.lobbyMemberId : event.lobbyMember2Id) ?? undefined
}
