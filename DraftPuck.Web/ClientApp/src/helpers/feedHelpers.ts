import { compareAsc } from 'date-fns'
import FeedItem from '@/models/feedItem'
import PlayType from '@/enums/playType'
import PeriodType from '@/enums/periodType'

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
      .map((play) => FeedItem.fromPlay(game.id, { away: game.awayTeam, home: game.homeTeam }, play, game.playerSummaries))
  )

  const eventItems = lobbyEvents.map((lobbyEvent) => FeedItem.fromLobbyEvent(lobbyEvent))

  return [...playItems, ...eventItems].sort((a, b) => compareAsc(a.time, b.time))
}
