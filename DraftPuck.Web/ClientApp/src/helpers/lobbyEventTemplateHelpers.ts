import LobbyEventTemplate from '@/models/lobbyEventTemplates'
import TeamColorLookup from '@/models/teamColorLookup'

export function parseLobbyEventText(lobbyEvent: LobbyEvent, lobby: Lobby, games: Game[]): LobbyEvent {
  const clone = { ...lobbyEvent }

  const templates = [
    new LobbyEventTemplate(['{{name}}', '{{senderName}}'], (token, eventText) => {
      const name = lobby.members.find((m) => m.id === lobbyEvent.lobbyMemberId)?.name ?? '(name)'
      return eventText.replace(token, `<strong>${name}</strong>`)
    }),
    new LobbyEventTemplate(['{{recipientName}}'], (token, eventText) => {
      const name = lobby.members.find((m) => m.id === lobbyEvent.lobbyMember2Id)?.name ?? '(recipient)'
      return eventText.replace(token, `<strong>${name}</strong>`)
    }),
    new LobbyEventTemplate(['{{player}}', '{{newScorer}}'], (token, eventText) => {
      const game = games.find((g) => g.id === lobbyEvent.gameId)
      if (!game) return eventText

      const player = game.playerSummaries.find((p) => p.id === lobbyEvent.playerId)
      if (!player) return 'Goal Changed'
      const name = `${player.firstName} ${player.lastName}`
      return eventText.replace(token, `<strong>${name}</strong>`)
    }),
    new LobbyEventTemplate(['{{player2}}', '{{oldScorer}}'], (token, eventText) => {
      const game = games.find((g) => g.id === lobbyEvent.gameId)
      if (!game) return eventText

      const player = game.playerSummaries.find((p) => p.id === lobbyEvent.player2Id)
      if (!player) return 'Goal Changed'
      const name = `${player.firstName} ${player.lastName}`
      return eventText.replace(token, `<strong>${name}</strong>`)
    }),
    new LobbyEventTemplate(['{{playerBadge}}'], (token, eventText) => {
      const game = games.find((g) => g.id === lobbyEvent.gameId)
      if (!game) return eventText

      const team = lobbyEvent.teamId === game.homeTeam.id ? game.homeTeam : game.awayTeam
      const logo = team.abbreviation === 'TBL' ? `/img/logos/${team.abbreviation}_LIGHT.png` : `/img/logos/${team.abbreviation}.png`

      const img = `<img style='height: 27px; width: 27px; margin-left: -20px; margin-right: -1px; margin-top: -12px; margin-bottom: -10px;' src="${logo}" />`
      const teamColor = TeamColorLookup[team.id]

      const playerLastName = game.playerSummaries.find((p) => p.id === lobbyEvent.playerId)?.lastName ?? '(Player)'
      return eventText.replace(
        token,
        `<span class='d-inline-block ps-3 ms-1 badge text-uppercase text-shadow' style='align-self: center; background-color: ${teamColor} !important;'>${img} ${playerLastName}</span>`
      )
    })
  ]

  clone.text = templates.reduce((templatedString, template) => template.replaceTokens(templatedString), clone.text)

  return clone
}
