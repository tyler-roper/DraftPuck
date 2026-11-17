/** How an award's icon moves on the post-game page (looping, small). */
export type AwardMotion = 'bob' | 'shiver' | 'beat' | 'jab' | 'clash' | 'swing' | 'wobble' | 'tick' | 'twinkle' | 'bounce'

export interface AwardStyle {
  /** A Flaticon solid-rounded icon. */
  icon: string
  color: string
  motion: AwardMotion
}

/** Each award's icon, accent colour and motion. Unknown awards get a twinkling star. */
const awardStyles: Record<string, AwardStyle> = {
  best_pick: { icon: 'trophy', color: '#ffb100', motion: 'bob' },
  hat_trick: { icon: 'hockey-sticks', color: '#ffb100', motion: 'clash' },
  sleeper: { icon: 'moon-stars', color: '#a78bfa', motion: 'twinkle' },
  perfect_night: { icon: 'crown', color: '#ffb100', motion: 'bob' },
  buzzer_beater: { icon: 'stopwatch', color: '#22c55e', motion: 'tick' },
  ice_cold: { icon: 'snowflake', color: '#00ccff', motion: 'shiver' },
  bully: { icon: 'hand-back-fist', color: '#dc3545', motion: 'jab' },
  rivalry: { icon: 'sword', color: '#f97316', motion: 'clash' },
  spreading_the_love: { icon: 'heart', color: '#ec4899', motion: 'beat' },
  thirstiest: { icon: 'glass-cheers', color: '#dc3545', motion: 'wobble' },
  designated_driver: { icon: 'car-side', color: '#22c55e', motion: 'bob' },
  hoarder: { icon: 'treasure-chest', color: '#f59e0b', motion: 'wobble' },
  chatterbox: { icon: 'comment-dots', color: '#38bdf8', motion: 'bounce' },
  silent_assassin: { icon: 'user-ninja', color: '#a8a29e', motion: 'twinkle' },
  beaten_by_a_robot: { icon: 'robot', color: '#a8a29e', motion: 'wobble' },
  over_achiever: { icon: 'medal', color: '#ffb100', motion: 'swing' }
}

export function awardStyle(key: string): AwardStyle {
  return awardStyles[key] ?? { icon: 'star', color: '#ffb100', motion: 'twinkle' }
}

export interface RecapStanding {
  name: string
  given: number
  taken: number
}

export interface RecapCard {
  heading: string
  subheading: string
  totals: Array<{ label: string; value: number }>
  awards: Array<{ title: string; winner: string; detail: string; color: string }>
  /** The top of the leaderboard, by drinks given. */
  standings: RecapStanding[]
}

/** How many awards and standings fit on the shared image. */
export const MAX_CARD_AWARDS = 5
export const MAX_CARD_STANDINGS = 3

/** The text of the shareable recap image, from what the post-game page already shows. */
export function buildRecapCard(
  joinCode: string,
  dateLabel: string,
  totals: { games: number; players: number; picks: number; drinks: number },
  awards: LobbyAward[],
  standings: RecapStanding[]
): RecapCard {
  return {
    heading: `Lobby ${joinCode}`,
    subheading: dateLabel,
    totals: [
      { label: 'Games', value: totals.games },
      { label: 'Players', value: totals.players },
      { label: 'Picks', value: totals.picks },
      { label: 'Drinks', value: totals.drinks }
    ],
    awards: awards.slice(0, MAX_CARD_AWARDS).map((a) => ({ title: a.title, winner: a.memberName, detail: a.detail, color: awardStyle(a.key).color })),
    standings: standings.slice(0, MAX_CARD_STANDINGS)
  }
}

export function recapFileName(joinCode: string) {
  return `draftpuck-${joinCode.toLowerCase()}-recap.png`
}
