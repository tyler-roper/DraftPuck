import TeamColorLookup from '@/models/teamColorLookup'

export function parseLobbyEventText(lobbyEvent: LobbyEvent, lobby: Lobby, games: Game[]): LobbyEvent {
  const clone = { ...lobbyEvent }

  const templates = [
    {
      strings: ['{{name}}', '{{senderName}}'],
      fill: (string: string, text: string): string => {
        const name = lobby.members.find((m) => m.id === lobbyEvent.lobbyMemberId)?.name ?? '(name)'
        return text.replace(string, `<strong>${name}</strong>`)
      }
    },
    {
      strings: ['{{recipientName}}'],
      fill: (string: string, text: string): string => {
        const name = lobby.members.find((m) => m.id === lobbyEvent.lobbyMember2Id)?.name ?? '(recipient)'
        return text.replace(string, `<strong>${name}</strong>`)
      }
    },
    {
      strings: ['{{player}}', '{{newScorer}}'],
      fill: (string: string, text: string): string => {
        const game = games.find((g) => g.id === lobbyEvent.gameId)
        if (!game) return text

        const player = game.playerSummaries.find((p) => p.id === lobbyEvent.playerId)
        return text.replace(string, `<strong>${player}</strong>`)
      }
    },
    {
      strings: ['{{player2}}', '{{oldScorer}}'],
      fill: (string: string, text: string): string => {
        const game = games.find((g) => g.id === lobbyEvent.gameId)
        if (!game) return text

        const player = game.playerSummaries.find((p) => p.id === lobbyEvent.player2Id)
        return text.replace(string, `<strong>${player}</strong>`)
      }
    },
    {
      strings: ['{{playerBadge}}'],
      fill: (string: string, text: string): string => {
        const game = games.find((g) => g.id === lobbyEvent.gameId)
        if (!game) return text

        const team = lobbyEvent.teamId === game.homeTeam.id ? game.homeTeam : game.awayTeam
        const logo = team.abbreviation === 'TBL' ? `/img/logos/${team.abbreviation}_LIGHT.png` : `/img/logos/${team.abbreviation}.png`

        const img = `<img style='height: 27px; width: 27px; margin-left: -20px; margin-right: -1px; margin-top: -12px; margin-bottom: -10px;' src="${logo}" />`
        const teamColor = TeamColorLookup[team.id]

        const playerLastName = game.playerSummaries.find((p) => p.id === lobbyEvent.playerId)?.lastName ?? '(Player)'
        return text.replace(
          string,
          `<span class='d-inline-block ps-3 ms-1 badge text-uppercase text-shadow' style='align-self: center; background-color: ${teamColor} !important;'>${img} ${playerLastName}</span>`
        )
      }
    }
  ]

  clone.text = templates.reduce(
    (text, template) =>
      template.strings.reduce((thisText, string) => {
        if (text.includes(string)) return template.fill(string, thisText)
        else return thisText
      }, text),
    clone.text
  )

  return clone
}
