import { escapeRegExp } from '@/helpers/htmlHelpers'

/**
 * Matches "@name" as its own word (start or whitespace before, end or whitespace after), case-insensitively.
 * Pass the name exactly as it appears in the text being searched (e.g. HTML-escaped when the text is escaped).
 * Captures: 1 = what precedes, 2 = the mention itself, 3 = what follows.
 */
export const mentionRegExp = (nameInText: string, flags = 'i') => new RegExp(`(^|\\s)(@${escapeRegExp(nameInText)})(\\s|$)`, flags)

/** Whether a chat message mentions this member. Names can contain spaces ("@Mc David"). */
export const mentionsMember = (messageText: string, memberName: string) => mentionRegExp(memberName).test(messageText)
