import TeamColorLookup from '@/models/teamColorLookup'
import FeedItemType from '@/enums/feedItemType'
import GoalTexts from '@/models/goalTexts'
import '@/extensions/arrayExtensions'
import type LobbyEventType from '@/enums/lobbyEventType'
import { getOrdinal } from '@/helpers/gameHelpers'
import PlayType from '@/enums/playType'

export default class FeedItem {
  gameId: number | undefined
  type: FeedItemType
  subType: PlayType | LobbyEventType
  time: Date
  title: string
  text: string
  subtext: string
  teamColor: string | undefined
  images: Array<string>
  player: PlayerSummary | undefined

  constructor(
    gameId: number | undefined,
    type: FeedItemType,
    subType: LobbyEventType,
    time: Date,
    title: string,
    text: string,
    subtext: string,
    teamColor: string | undefined,
    images: Array<string>,
    player: PlayerSummary | undefined
  ) {
    this.gameId = gameId
    this.type = type
    this.subType = subType
    this.time = time
    this.title = title
    this.text = text
    this.subtext = subtext
    this.teamColor = teamColor
    this.images = images
    this.player = player
  }

  get isGoal() {
    return this.subType === PlayType.Goal
  }

  get isPenalty() {
    return this.subType === PlayType.Penalty
  }

  get isScoringPlay() {
    return this.isPenalty || this.isGoal
  }

  static camelCaseToTitleCase(camelCaseString: string) {
    let result = camelCaseString.replace(/([A-Z])/g, ' $1')
    result = result.trim().toLowerCase()
    result = result.charAt(0).toUpperCase() + result.slice(1)
    return result.replace(/\s+([a-z])/g, (_, p1) => {
      return ' ' + p1.toUpperCase()
    })
  }

  static fromPlay(gameId: number, teams: { home: GameTeam; away: GameTeam }, play: Play, players: PlayerSummary[]) {
    const homeAbbreviation = teams.home.abbreviation
    const awayAbbreviation = teams.away.abbreviation

    const player = players.find((p) => p.id === play.primaryPlayerId)

    let title = PlayType[play.type].replace(/([a-z])([A-Z])/g, '$1 $2')
    let subtext = `${play.timeInPeriod} ${getOrdinal(play.period, play.periodType)}`
    let teamColor: string | undefined = undefined
    let images: Array<string> = [`${homeAbbreviation}.png`, `${awayAbbreviation}.png`]
    let text = ''

    //set color, player, image
    if (play.type === PlayType.Penalty || play.type === PlayType.Goal) {
      teamColor = TeamColorLookup[play.primaryTeamId!]
      const scoringTeamAbbreviation = play.primaryTeamId! === teams.home.id ? homeAbbreviation : awayAbbreviation

      if (scoringTeamAbbreviation) {
        if (play.type === PlayType.Goal && scoringTeamAbbreviation.toLowerCase() === 'tbl') images = [`${scoringTeamAbbreviation}_LIGHT.png`]
        else images = [`${scoringTeamAbbreviation}.png`]
      }
    }

    //set title
    if (play.type === PlayType.Goal) {
      const winningScore = Math.max(play.awayScore, play.homeScore)
      const losingScore = Math.min(play.awayScore, play.homeScore)

      if (winningScore === losingScore) {
        title = `${winningScore}-${losingScore} TIE`
      } else {
        const homeTeamIsWinning = winningScore === play.homeScore
        title = homeTeamIsWinning ? `${winningScore}-${losingScore} ${homeAbbreviation}` : `${winningScore}-${losingScore} ${awayAbbreviation}`
      }

      if (player != null) text = this.getRandomGoalText(`${player.firstName} ${player.lastName}`, play)
      else text = 'Scorer not yet assigned...'
    }

    if (play.type === PlayType.Penalty) {
      if (player && play.penalty) text = `${player.firstName} ${player.lastName} - ${this.camelCaseToTitleCase(play.penalty)}`
      else text = 'Waiting for details...'
    }

    if (play.type === PlayType.Challenge) {
      if (play.primaryTeamId) {
        const challengingTeamAbbreviation = play.primaryTeamId === teams.home.id ? homeAbbreviation : awayAbbreviation

        text = `${challengingTeamAbbreviation} are challenging the play.`
        images = [`${challengingTeamAbbreviation}.png`]
      }
    }

    //set subtext
    if (play.type === PlayType.PeriodStart || play.type === PlayType.PeriodEnd) {
      subtext = ''
      text = play.type === PlayType.PeriodStart ? 'Start of ' : 'End of '
      text += getOrdinal(play.period, play.periodType)
      if (play.period <= 3) text += ' Period'
    }

    if (play.type === PlayType.GameEnd) {
      const winningScore = Math.max(teams.away.score, teams.home.score)
      const losingScore = Math.min(teams.away.score, teams.home.score)
      const homeTeamIsWinning = winningScore === teams.home.score
      text = homeTeamIsWinning ? `${homeAbbreviation} wins ${winningScore}-${losingScore}` : `${awayAbbreviation} wins ${winningScore}-${losingScore}`
    }

    return new this(gameId, FeedItemType.GameEvent, play.type, play.dateTime, title, text, subtext, teamColor, images, player)
  }

  static fromLobbyEvent(lobbyEvent: LobbyEvent) {
    return new this(
      lobbyEvent.gameId!,
      FeedItemType.LobbyEvent,
      lobbyEvent.lobbyEventType,
      lobbyEvent.timeUtc,
      lobbyEvent.title,
      lobbyEvent.text,
      lobbyEvent.subtext!,
      lobbyEvent.teamColor,
      [],
      undefined
    )
  }

  private static getRandomGoalText(playerName: string, play: Play): string {
    const seed = Math.floor(Number(play.timeInPeriod.split(':')[1]))
    const randomString = GoalTexts.seed(seed)
    const replaced = randomString.replace('{{player}}', `<strong>${playerName}</strong>`)

    return `&#x1F6A8; ${replaced}!`
  }
}
