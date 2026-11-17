import TeamColorLookup from '@/models/teamColorLookup'

/** A drink handed out: who gave it, who got it, when, the player whose goal earned it, and what the giver said. */
export interface TimelineDrink {
  id: string
  time: Date
  giver: LobbyMember
  recipient: LobbyMember
  player?: { lastName: string; abbreviation: string; color?: string }
  message?: string
}

/** Every drink handed out in the lobby, newest first (by when it was handed out). */
export function drinkTimeline(lobby: Lobby, games: Game[]): TimelineDrink[] {
  const members = new Map(lobby.members.map((m) => [m.id, m]))

  return lobby.members
    .flatMap((giver) =>
      giver.picks
        .filter((pick) => pick.isActive)
        .flatMap((pick) =>
          pick.drinks
            .filter((drink) => drink.recipientLobbyMemberId && members.has(drink.recipientLobbyMemberId))
            .map((drink) => ({
              id: drink.id,
              time: drink.assigned ?? drink.created,
              giver,
              recipient: members.get(drink.recipientLobbyMemberId)!,
              player: playerOf(games, pick),
              message: drink.message ?? undefined
            }))
        )
    )
    .sort((a, b) => Number(b.time) - Number(a.time))
}

function playerOf(games: Game[], pick: LobbyMemberPick) {
  const game = games.find((g) => g.id === pick.gameId)
  if (!game) return undefined
  const player = [...game.playerSummaries, ...(game.formerPlayers ?? [])].find((p) => p.id === pick.playerId)
  const team = [game.homeTeam, game.awayTeam].find((t) => t.id === pick.teamId)
  if (!player || !team) return undefined
  return { lastName: player.lastName, abbreviation: team.abbreviation, color: TeamColorLookup[team.id] }
}

/** How many drinks each member has given and taken. */
export interface DrinkTally {
  member: LobbyMember
  given: number
  taken: number
}

/** Everyone who has given or taken a drink, the most-punished first (then the most generous, then by name). */
export function drinkTallies(drinks: TimelineDrink[]): DrinkTally[] {
  const tallies = new Map<string, DrinkTally>()
  const tally = (member: LobbyMember) => tallies.get(member.id) ?? tallies.set(member.id, { member, given: 0, taken: 0 }).get(member.id)!

  for (const drink of drinks) {
    tally(drink.giver).given++
    tally(drink.recipient).taken++
  }

  return [...tallies.values()].sort((a, b) => b.taken - a.taken || b.given - a.given || a.member.name.localeCompare(b.member.name))
}
