import { escapeRegExp } from '@/helpers/htmlHelpers'

/**
 * Matches "@name" as its own word (start or whitespace before, end or whitespace after), case-insensitively.
 * Pass the name exactly as it appears in the text being searched (e.g. HTML-escaped when the text is escaped).
 * Captures: 1 = what precedes, 2 = the mention itself, 3 = what follows.
 */
export const mentionRegExp = (nameInText: string, flags = 'i') => new RegExp(`(^|\\s)(@${escapeRegExp(nameInText)})(\\s|$)`, flags)

/** Whether a chat message mentions this member. Names can contain spaces ("@Mc David"). */
export const mentionsMember = (messageText: string, memberName: string) => mentionRegExp(memberName).test(messageText)

/** An "@..." being typed at the cursor: where its "@" is, and what follows it so far. */
export interface MentionQuery {
  start: number
  query: string
}

/** The longest a mention being typed can get (names can be up to 50 characters). */
const MAX_MENTION_QUERY = 50

/**
 * The mention being typed at the cursor, if any: an "@" at the start or after whitespace, followed (up to the cursor)
 * by anything but a line break. Names can contain spaces, so a space doesn't end it; `matchMentions` decides whether
 * anyone still matches.
 */
export function findMentionQuery(text: string, cursor: number): MentionQuery | undefined {
  const before = text.slice(0, cursor)
  const at = before.lastIndexOf('@')
  if (at < 0 || (at > 0 && !/\s/.test(before[at - 1]))) return undefined

  const query = before.slice(at + 1)
  if (query.length > MAX_MENTION_QUERY || /[\r\n]/.test(query) || query.startsWith(' ')) return undefined
  return { start: at, query }
}

/** Where the query matches in a name: the whole name from the start, or any word in it. */
export function mentionMatchIndex(name: string, query: string): number {
  if (query.length === 0) return 0
  const lowerName = name.toLowerCase()
  const lowerQuery = query.toLowerCase()
  if (lowerName.startsWith(lowerQuery)) return 0

  for (let i = 1; i < name.length; i++)
    if (/\s/.test(name[i - 1]) && lowerName.startsWith(lowerQuery, i)) return i
  return -1
}

/**
 * The members a mention could be for, best first: names starting with the query, then names with a word starting
 * with it, alphabetically within each. Everyone (alphabetically) for a bare "@".
 */
export function matchMentions<T extends { name: string }>(members: T[], query: string, limit = 20): T[] {
  return members
    .map((member) => ({ member, index: mentionMatchIndex(member.name, query) }))
    .filter((m) => m.index >= 0)
    .sort((a, b) => Number(a.index > 0) - Number(b.index > 0) || a.member.name.localeCompare(b.member.name))
    .slice(0, limit)
    .map((m) => m.member)
}

/** The text with the mention being typed replaced by "@Full Name ", and where the cursor goes (after the space). */
export function applyMention(text: string, mention: MentionQuery, cursor: number, name: string) {
  const inserted = `@${name} `
  // Don't double the space if one already follows.
  const after = text.slice(cursor).replace(/^ /, '')
  return { text: text.slice(0, mention.start) + inserted + after, cursor: mention.start + inserted.length }
}
