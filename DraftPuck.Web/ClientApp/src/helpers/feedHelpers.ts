import { compareAsc } from 'date-fns'
import FeedItem from '@/models/feedItem'
import PlayType from '@/enums/playType'
import PeriodType from '@/enums/periodType'
import LobbyEventType from '@/enums/lobbyEventType'

const FEED_PLAY_TYPES = [PlayType.Goal, PlayType.PeriodStart, PlayType.PeriodEnd, PlayType.GameEnd, PlayType.Challenge, PlayType.Penalty]

/**
 * A lobby's feed: the notable plays from every game since the lobby was created (all of them in test mode, where game
 * times are simulated), plus the lobby's own events, oldest first. Shootout goals are left out, since they don't count.
 * Lobby events must already have their template text filled in.
 */
export function buildFeedItems(games: Game[], lobbyEvents: LobbyEvent[], lobbyCreated: Date, includePlaysBeforeLobby: boolean): FeedItem[] {
  const playItems = games.flatMap((game) =>
    game.plays
      .filter(
        (play) =>
          FEED_PLAY_TYPES.includes(play.type) &&
          (includePlaysBeforeLobby || play.dateTime >= lobbyCreated) &&
          !(play.type === PlayType.Goal && play.periodType === PeriodType.Shootout)
      )
      .map((play) => FeedItem.fromPlay(game.id, { away: game.awayTeam, home: game.homeTeam }, play, game.playerSummaries, game.plays))
  )

  const eventItems = lobbyEvents.map((lobbyEvent) => FeedItem.fromLobbyEvent(lobbyEvent))

  return [...playItems, ...eventItems].sort((a, b) => compareAsc(a.time, b.time))
}

/** Drink events that belong to a goal (it earned the drink); they link back to it. */
const DRINK_EVENT_TYPES = [LobbyEventType.DrinkAwarded, LobbyEventType.DrinkAssigned, LobbyEventType.DrinkInvalidated, LobbyEventType.DrinkRevoked]

/** The key of the goal a drink event came from (its play), or undefined for other items. */
export function goalKeyFor(item: FeedItem): string | undefined {
  const event = item.lobbyEvent
  if (!event || !DRINK_EVENT_TYPES.includes(event.lobbyEventType) || event.gameId == null || event.gameEventId == null) return undefined
  return `play-${event.gameId}-${event.gameEventId}`
}

/** A goal and every drink event that came from it: what lights up when you follow a link. */
export function chainKeys(items: FeedItem[], goalKey: string): string[] {
  return [goalKey, ...items.filter((item) => goalKeyFor(item) === goalKey).map((item) => item.key)]
}

/** Where a goal's drinks went: who earned each, and who it was handed to (if it has been). */
export interface GoalDrink {
  earner: string
  recipient?: string
}

export function goalDrinks(lobby: Lobby, gameId: number, playId: number): GoalDrink[] {
  const names = new Map(lobby.members.map((m) => [m.id, m.name]))
  return lobby.members.flatMap((member) =>
    member.picks
      .filter((pick) => pick.isActive && pick.gameId === gameId)
      .flatMap((pick) => pick.drinks.filter((drink) => drink.eventId === playId))
      .map((drink) => ({ earner: member.name, recipient: drink.recipientLobbyMemberId ? names.get(drink.recipientLobbyMemberId) : undefined }))
  )
}
