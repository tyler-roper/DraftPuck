import { computed, type Ref } from 'vue'
import { compareAsc } from 'date-fns'
import FeedItem from '@/models/feedItem'
import PlayType from '@/enums/playType'
import PeriodType from '@/enums/periodType'
import { parseLobbyEventText } from '@/helpers/lobbyEventTemplateHelpers'

export function useFeedItems(
  lobby: Ref<Lobby | undefined>,
  games: Ref<Game[]>,
  events: Ref<LobbyEvent[]>,
  appIsTestMode: Ref<boolean>
) {
  const mappedEvents = computed(() =>
    events.value.map((e) => parseLobbyEventText(e, lobby.value!, games.value))
  )

  const feedItems = computed(() => {
    if (!lobby.value) return []
    const desiredPlayTypes = [
      PlayType.Goal,
      PlayType.PeriodStart,
      PlayType.PeriodEnd,
      PlayType.GameEnd,
      PlayType.Challenge,
      PlayType.Penalty
    ]

    const gameItems = games.value.flatMap((game) => {
      return game.plays.reduce((items: FeedItem[], play) => {
        const includedInFilters = desiredPlayTypes.includes(play.type)
        const happenedAfterLobbyStarted = appIsTestMode.value ? true : play.dateTime >= lobby.value!.created
        const isShootoutGoal = play.type === PlayType.Goal && play.periodType === PeriodType.Shootout
        if (includedInFilters && happenedAfterLobbyStarted && !isShootoutGoal) {
          return [
            ...items,
            FeedItem.fromPlay(game.id, { away: game.awayTeam, home: game.homeTeam }, play, game.playerSummaries)
          ]
        } else return items
      }, [])
    })

    const lobbyItems = mappedEvents.value.map((evt) => FeedItem.fromLobbyEvent(evt))
    const allFeedItems = [...gameItems, ...lobbyItems]
    allFeedItems.sort((a, b) => compareAsc(a.time, b.time))
    return allFeedItems
  })

  return {
    feedItems,
    mappedEvents
  }
}
