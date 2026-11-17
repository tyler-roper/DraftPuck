/** A drink handed out: who earned it (the giver), who took it, the pick that earned it, and when. */
interface GivenDrink {
  giver: LobbyMember
  recipient: LobbyMember
  pick: LobbyMemberPick
  earned: Date
  handedOut: Date
}

export interface PickStat {
  name: string
  team: string
  drinks: number
}

export interface MemberCount {
  member: LobbyMember
  count: number
}

/** One member's night: what each pick earned (best first), who they made drink, and who made them drink. */
export interface MemberNight {
  picks: PickStat[]
  gaveTo: MemberCount[]
  gotFrom: MemberCount[]
}

/** A drink handed out in a stretch of the night: who gave it, who took it, and the player whose goal earned it. */
export interface ChartDrink {
  giver: LobbyMember
  recipient: LobbyMember
  player?: string
}

/** A stretch of the night: the drinks handed out in it, and how many involved the chosen member. */
export interface ChartBar {
  start: Date
  end: Date
  total: number
  involving: number
  drinks: ChartDrink[]
}

const playerKey = (gameId: number, playerId: number) => `${gameId}-${playerId}`

function playerLookup(players: RecapPlayer[]) {
  const byKey = new Map(players.map((p) => [playerKey(p.gameId, p.playerId), p]))
  return (pick: LobbyMemberPick) => byKey.get(playerKey(pick.gameId, pick.playerId))
}

/** Every drink handed out on an active pick, oldest first. */
function givenDrinks(lobby: Lobby): GivenDrink[] {
  const members = new Map(lobby.members.map((m) => [m.id, m]))
  return lobby.members
    .flatMap((giver) =>
      giver.picks
        .filter((pick) => pick.isActive)
        .flatMap((pick) =>
          pick.drinks
            .filter((d) => d.recipientLobbyMemberId && members.has(d.recipientLobbyMemberId))
            .map((d) => ({
              giver,
              recipient: members.get(d.recipientLobbyMemberId)!,
              pick,
              earned: new Date(d.created),
              handedOut: new Date(d.assigned ?? d.created)
            }))
        )
    )
    .sort((a, b) => Number(a.handedOut) - Number(b.handedOut))
}

function counts(members: LobbyMember[]): MemberCount[] {
  const byId = new Map<string, MemberCount>()
  for (const member of members) {
    const entry = byId.get(member.id) ?? { member, count: 0 }
    entry.count++
    byId.set(member.id, entry)
  }
  return [...byId.values()].sort((a, b) => b.count - a.count || a.member.name.localeCompare(b.member.name))
}

export function memberNight(lobby: Lobby, players: RecapPlayer[], memberId: string): MemberNight {
  const member = lobby.members.find((m) => m.id === memberId)
  const playerOf = playerLookup(players)
  const drinks = givenDrinks(lobby)

  const picks = (member?.picks ?? [])
    .filter((pick) => pick.isActive)
    .map((pick) => {
      const player = playerOf(pick)
      return { name: player?.name ?? 'Unknown player', team: player?.team ?? '', drinks: pick.drinks.length }
    })
    .sort((a, b) => b.drinks - a.drinks || a.name.localeCompare(b.name))

  return {
    picks,
    gaveTo: counts(drinks.filter((d) => d.giver.id === memberId).map((d) => d.recipient)),
    gotFrom: counts(drinks.filter((d) => d.recipient.id === memberId).map((d) => d.giver))
  }
}

/**
 * The night in stretches of `minutes`, from the first drink handed out to the last, with how many involved the member
 * (given or taken). Empty when nobody drank.
 */
export function nightChart(lobby: Lobby, players: RecapPlayer[], memberId: string, minutes = 10): ChartBar[] {
  const drinks = givenDrinks(lobby)
  const playerOf = playerLookup(players)
  if (!drinks.length) return []

  const size = minutes * 60_000
  const first = Math.floor(Number(drinks[0].handedOut) / size) * size
  const last = Number(drinks[drinks.length - 1].handedOut)
  const bars: ChartBar[] = []
  for (let start = first; start <= last; start += size)
    bars.push({ start: new Date(start), end: new Date(start + size), total: 0, involving: 0, drinks: [] })

  for (const d of drinks) {
    const bar = bars[Math.floor((Number(d.handedOut) - first) / size)]
    bar.total++
    bar.drinks.push({ giver: d.giver, recipient: d.recipient, player: playerOf(d.pick)?.name })
    if (d.giver.id === memberId || d.recipient.id === memberId) bar.involving++
  }
  return bars
}
