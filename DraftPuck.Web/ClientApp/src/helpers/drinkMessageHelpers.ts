/** As long as the server allows (DrinkMessages.MaxLength). */
export const DRINK_MESSAGE_MAX_LENGTH = 80

/** One-tap messages offered when giving a drink. */
export const DRINK_MESSAGE_SUGGESTIONS = ['Cheers 🍻', 'Revenge.', 'Drink up!', 'Nothing personal.', 'You earned this.']

/** The message as the server will store it: whitespace collapsed and trimmed, or undefined when there's nothing to say. */
export function tidyDrinkMessage(message: string): string | undefined {
  const tidy = message.replace(/\s+/g, ' ').trim()
  return tidy.length > 0 ? tidy : undefined
}

/**
 * The message that came with a drink handed out (a DrinkAssigned event), if any: the drink earned by the sender's
 * pick for the same goal, given to the event's recipient.
 */
export function messageForDrinkEvent(lobby: Lobby, event: LobbyEvent): string | undefined {
  const sender = lobby.members.find((m) => m.id === event.lobbyMemberId)
  const drink = sender?.picks
    .flatMap((p) => p.drinks)
    .find((d) => d.eventId === event.gameEventId && d.recipientLobbyMemberId === event.lobbyMember2Id)
  return drink?.message ?? undefined
}
