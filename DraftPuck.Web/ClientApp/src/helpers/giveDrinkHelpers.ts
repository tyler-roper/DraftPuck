/**
 * Giving out drinks: the drinks your picks earned that nobody has been given yet, who can take one, and whether the
 * give-a-drink bar is shrunk to its chip. Dismissing the bar shrinks it for the drinks you had then; a new drink brings
 * the full bar back.
 */

/** The member's drinks still to give out, oldest pick first (the order they're handed out in). */
export function drinksToGive(member: LobbyMember | undefined): Drink[] {
  return member?.picks.flatMap((p) => p.drinks.filter((d) => !d.recipientLobbyMemberId)) ?? []
}

/** Who can be handed a drink: everyone in the lobby but you and the bots, in the lobby's order. */
export function drinkRecipients(lobby: Lobby, giverMemberId: string): LobbyMember[] {
  return lobby.members.filter((m) => m.id !== giverMemberId && !m.isBot && !m.isRemoved)
}

/** Shrunk to the chip while every drink to give was already there when the bar was dismissed. */
export function isGiveBarCollapsed(drinkIds: string[], dismissedDrinkIds: string[]): boolean {
  const dismissed = new Set(dismissedDrinkIds)
  return drinkIds.length > 0 && drinkIds.every((id) => dismissed.has(id))
}
