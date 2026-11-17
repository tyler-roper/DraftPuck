import { escapeHtml } from '@/helpers/htmlHelpers'
import TeamColorLookup from '@/models/teamColorLookup'
import FeedItemType from '@/enums/feedItemType'
import GoalTexts from '@/models/goalTexts'
import '@/extensions/arrayExtensions'
import LobbyEventType from '@/enums/lobbyEventType'
import { getOrdinal } from '@/helpers/gameHelpers'
import PlayType from '@/enums/playType'
import PeriodType from '@/enums/periodType'

/**
 * How an item is drawn. Game events have a team block on the left: goals in the team's colour (the loudest thing in the
 * feed), penalties and challenges quietly, the game's state (periods) and its final score with both logos. Lobby
 * events have a round icon or avatar instead.
 */
export type FeedLayout = 'goal' | 'game' | 'state' | 'final' | 'lobby'

/** A team as the feed shows it: its logo file and primary colour. */
export interface FeedTeam {
  abbreviation: string
  logo: string
  color: string | undefined
}

interface FeedItemInit {
  key: string
  gameId: number | undefined
  type: FeedItemType
  subType: PlayType | LobbyEventType
  layout: FeedLayout
  time: Date
  title: string
  text: string
  detail?: string
  teams?: FeedTeam[]
  playId?: number
  lobbyEvent?: LobbyEvent
}

/** Tampa Bay's logo is dark on dark, so the feed uses its light version. */
const logoFor = (abbreviation: string) => (abbreviation.toLowerCase() === 'tbl' ? `${abbreviation}_LIGHT.png` : `${abbreviation}.png`)

const feedTeam = (team: GameTeam): FeedTeam => ({ abbreviation: team.abbreviation, logo: logoFor(team.abbreviation), color: TeamColorLookup[team.id] })

export default class FeedItem {
  /** Identifies the play or lobby event across refetches, so lists can tell new items from existing ones. */
  key: string
  gameId: number | undefined
  type: FeedItemType
  subType: PlayType | LobbyEventType
  layout: FeedLayout
  time: Date
  /** The small uppercase line at the top: "Goal · EDM 2–1", "Penalty · CGY", "Drink Awarded". */
  title: string
  /** The main line, as HTML (names and players are escaped). */
  text: string
  /** A quieter line under it: when in the game, or who starred in a final. */
  detail: string
  /** The team (goals, penalties) or both teams (periods, finals) whose logos the item shows. */
  teams: FeedTeam[]
  /** The play's id, for game events (a goal's drinks point back to it). */
  playId: number | undefined
  /** The lobby event behind the item, for lobby events. */
  lobbyEvent: LobbyEvent | undefined

  constructor(init: FeedItemInit) {
    this.key = init.key
    this.gameId = init.gameId
    this.type = init.type
    this.subType = init.subType
    this.layout = init.layout
    this.time = init.time
    this.title = init.title
    this.text = init.text
    this.detail = init.detail ?? ''
    this.teams = init.teams ?? []
    this.playId = init.playId
    this.lobbyEvent = init.lobbyEvent
  }

  get isGoal() {
    return this.subType === PlayType.Goal && this.type === FeedItemType.GameEvent
  }

  static camelCaseToTitleCase(camelCaseString: string) {
    let result = camelCaseString.replace(/([A-Z])/g, ' $1')
    result = result.trim().toLowerCase()
    result = result.charAt(0).toUpperCase() + result.slice(1)
    return result.replace(/\s+([a-z])/g, (_, p1) => {
      return ' ' + p1.toUpperCase()
    })
  }

  /** @param plays The game's plays, for a final's standout scorers. */
  static fromPlay(gameId: number, teams: { home: GameTeam; away: GameTeam }, play: Play, players: PlayerSummary[], plays: Play[] = []) {
    const home = feedTeam(teams.home)
    const away = feedTeam(teams.away)
    const playerName = (id: number | null) => {
      const player = players.find((p) => p.id === id)
      return player ? `${player.firstName} ${player.lastName}` : undefined
    }
    const teamOf = (teamId: number | null) => (teamId === teams.home.id ? home : away)
    const when = `${getOrdinal(play.period, play.periodType)} ${play.timeInPeriod}`
    // "EDM 1–0 CGY", away team first, as in "EDM @ CGY".
    const gameScore = `${away.abbreviation} ${play.awayScore}–${play.homeScore} ${home.abbreviation}`
    const base = { key: `play-${gameId}-${play.id}`, gameId, type: FeedItemType.GameEvent, subType: play.type, time: play.dateTime, playId: play.id }

    switch (play.type) {
      case PlayType.Goal: {
        const scorer = teamOf(play.primaryTeamId)
        const isHome = scorer === home
        const [ours, theirs] = isHome ? [play.homeScore, play.awayScore] : [play.awayScore, play.homeScore]
        const name = playerName(play.primaryPlayerId)
        return new this({
          ...base,
          layout: 'goal',
          title: `Goal · ${scorer.abbreviation} ${ours}–${theirs}`,
          text: name ? this.getRandomGoalText(name, play) : 'Scorer not yet assigned…',
          detail: when,
          teams: [scorer]
        })
      }
      case PlayType.Penalty: {
        const team = teamOf(play.primaryTeamId)
        const name = playerName(play.primaryPlayerId)
        return new this({
          ...base,
          layout: 'game',
          title: `Penalty · ${team.abbreviation}`,
          text: name && play.penalty ? `<strong>${escapeHtml(name)}</strong> · ${escapeHtml(this.camelCaseToTitleCase(play.penalty))}` : 'Waiting for details…',
          detail: when,
          teams: [team]
        })
      }
      case PlayType.Challenge: {
        const team = play.primaryTeamId ? teamOf(play.primaryTeamId) : undefined
        return new this({
          ...base,
          layout: team ? 'game' : 'state',
          title: team ? `Coach's challenge · ${team.abbreviation}` : "Coach's challenge",
          text: team ? `<strong>${team.abbreviation}</strong> are challenging the play.` : 'The play is being challenged.',
          detail: when,
          teams: team ? [team] : [away, home]
        })
      }
      case PlayType.GameEnd: {
        const homeWon = teams.home.score > teams.away.score
        const [winner, loser] = homeWon ? [home, away] : [away, home]
        const [high, low] = homeWon ? [teams.home.score, teams.away.score] : [teams.away.score, teams.home.score]
        return new this({
          ...base,
          layout: 'final',
          title: 'Final',
          text: `${winner.abbreviation} ${high} <span class="loser">– ${low} ${loser.abbreviation}</span>`,
          detail: this.standoutScorers(plays, players),
          teams: [away, home]
        })
      }
      default: {
        // Period starts and ends: the game's state, with its score.
        const label = `${play.type === PlayType.PeriodStart ? 'Start' : 'End'} of ${getOrdinal(play.period, play.periodType)}`
        return new this({ ...base, layout: 'state', title: label, text: `<strong>${gameScore}</strong>`, teams: [away, home] })
      }
    }
  }

  static fromLobbyEvent(lobbyEvent: LobbyEvent) {
    return new this({
      key: `event-${lobbyEvent.id}`,
      gameId: lobbyEvent.gameId ?? undefined,
      type: FeedItemType.LobbyEvent,
      subType: lobbyEvent.lobbyEventType,
      layout: 'lobby',
      time: lobbyEvent.timeUtc,
      title: lobbyEvent.title,
      text: lobbyEvent.text,
      lobbyEvent
    })
  }

  /** "McDavid 2G · Draisaitl 2G": players who scored more than once in the game, best first. */
  private static standoutScorers(plays: Play[], players: PlayerSummary[]) {
    const goals = new Map<number, number>()
    for (const play of plays)
      if (play.type === PlayType.Goal && play.primaryPlayerId != null && play.periodType !== PeriodType.Shootout) goals.set(play.primaryPlayerId, (goals.get(play.primaryPlayerId) ?? 0) + 1)

    return [...goals.entries()]
      .filter(([, count]) => count > 1)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 2)
      .map(([id, count]) => `${escapeHtml(players.find((p) => p.id === id)?.lastName ?? '?')} ${count}G`)
      .join(' · ')
  }

  /** One of the goal lines, chosen by the play, so a goal keeps its line across refreshes and goals vary. */
  private static getRandomGoalText(playerName: string, play: Play): string {
    const randomString = GoalTexts.seed(play.id)
    return randomString.replace('{{player}}', `<strong>${escapeHtml(playerName)}</strong>`)
  }
}
