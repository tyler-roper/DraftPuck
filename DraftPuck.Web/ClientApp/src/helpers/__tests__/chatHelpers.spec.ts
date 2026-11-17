import { describe, expect, it, vi } from 'vitest'
import { applyMention, findMentionQuery, matchMentions, mentionMatchIndex, mentionRegExp, mentionsMember } from '@/helpers/chatHelpers'
import { runChatCommand, type ChatCommandContext } from '@/helpers/chatCommands'

describe('mentionsMember', () => {
  it('finds @name as its own word, in any case', () => {
    expect(mentionsMember('nice one @sam', 'Sam')).toBe(true)
    expect(mentionsMember('@SAM look', 'Sam')).toBe(true)
    expect(mentionsMember('email sam@sam.com', 'Sam')).toBe(false)
    expect(mentionsMember('@samantha hi', 'Sam')).toBe(false)
  })

  it('handles names with spaces and regex characters', () => {
    expect(mentionsMember('hey @Mc David!', 'Mc David')).toBe(false) // "!" is attached
    expect(mentionsMember('hey @Mc David you there', 'Mc David')).toBe(true)
    expect(mentionsMember('@C.J. go', 'C.J.')).toBe(true)
    expect(mentionsMember('@CxJx go', 'C.J.')).toBe(false)
  })

  it('can replace every mention when given the global flag', () => {
    expect('@a and @A'.replace(mentionRegExp('a', 'ig'), '$1<b>$2</b>$3')).toBe('<b>@a</b> and <b>@A</b>')
  })
})

describe('runChatCommand', () => {
  function context(): ChatCommandContext & { output: string[] } {
    const output: string[] = []
    const me = { id: 'm1', name: 'Sam', messages: [{}], picks: [{}] } as unknown as LobbyMember
    const other = { id: 'm2', name: 'Mc David', messages: [], picks: [] } as unknown as LobbyMember
    return {
      output,
      lobby: { members: [me, other] } as unknown as Lobby,
      currentMember: me,
      setDebugging: vi.fn(),
      sendSystemMessage: (text) => output.push(text),
      describeConnection: () => ({ state: 'Connected' })
    }
  }

  it('/debug sets a level from 1 to 3, defaults to 1, and 0 turns it off', () => {
    const ctx = context()

    runChatCommand('debug', [], ctx)
    runChatCommand('debug', ['7'], ctx)
    runChatCommand('debug', ['0'], ctx)

    expect(vi.mocked(ctx.setDebugging).mock.calls.map(([level]) => level)).toEqual([1, 3, 0])
    expect(ctx.output).toEqual(['Debugging enabled (Level 1).', 'Debugging enabled (Level 3).', 'Debugging disabled.'])
  })

  it('/me and /user show member details without messages or picks', () => {
    const ctx = context()

    runChatCommand('me', [], ctx)
    runChatCommand('user', ['mc', 'DAVID'], ctx)
    runChatCommand('user', ['nobody'], ctx)

    expect(JSON.parse(ctx.output[0])).toEqual({ id: 'm1', name: 'Sam' })
    expect(JSON.parse(ctx.output[1])).toEqual({ id: 'm2', name: 'Mc David' })
    expect(ctx.output[2]).toBe('User nobody not found.')
  })

  it('/connection describes the live connection', () => {
    const ctx = context()
    runChatCommand('connection', [], ctx)
    expect(JSON.parse(ctx.output[0])).toEqual({ state: 'Connected' })
  })

  it('ignores unknown commands', () => {
    const ctx = context()
    expect(runChatCommand('nope', [], ctx)).toBe(false)
    expect(ctx.output).toEqual([])
  })
})

describe('findMentionQuery', () => {
  it('finds the @ being typed at the cursor, spaces and all', () => {
    expect(findMentionQuery('hey @mc', 7)).toEqual({ start: 4, query: 'mc' })
    expect(findMentionQuery('@Mc Da', 6)).toEqual({ start: 0, query: 'Mc Da' })
    expect(findMentionQuery('hey @', 5)).toEqual({ start: 4, query: '' })
  })

  it('ignores an @ inside a word, after a line break, or followed by a space', () => {
    expect(findMentionQuery('me@mail', 7)).toBeUndefined()
    expect(findMentionQuery('@sam\nhi', 7)).toBeUndefined()
    expect(findMentionQuery('@ hi', 4)).toBeUndefined()
    expect(findMentionQuery('no mention', 10)).toBeUndefined()
  })

  it('only looks before the cursor', () => {
    expect(findMentionQuery('@sam later', 2)).toEqual({ start: 0, query: 's' })
  })
})

describe('matchMentions', () => {
  const members = ['Mc David', 'Sam', 'Davey Jones', 'Alex Sampson'].map((name) => ({ name }))
  const names = (query: string) => matchMentions(members, query).map((m) => m.name)

  it('puts names starting with the query first, then names with a word starting with it', () => {
    expect(names('da')).toEqual(['Davey Jones', 'Mc David'])
    expect(names('sam')).toEqual(['Sam', 'Alex Sampson'])
  })

  it('matches case-insensitively, across spaces, and lists everyone for a bare @', () => {
    expect(names('MC D')).toEqual(['Mc David'])
    expect(names('')).toEqual(['Alex Sampson', 'Davey Jones', 'Mc David', 'Sam'])
    expect(names('zz')).toEqual([])
  })

  it('knows where the match is, for highlighting', () => {
    expect(mentionMatchIndex('Mc David', 'dav')).toBe(3)
    expect(mentionMatchIndex('Mc David', 'mc')).toBe(0)
    expect(mentionMatchIndex('Mc David', 'avid')).toBe(-1)
  })
})

describe('applyMention', () => {
  it('replaces what was typed with the full name and a space, and moves the cursor after it', () => {
    expect(applyMention('hey @mc', { start: 4, query: 'mc' }, 7, 'Mc David')).toEqual({ text: 'hey @Mc David ', cursor: 14 })
  })

  it('keeps the rest of the message, without doubling a space', () => {
    expect(applyMention('@sa you there', { start: 0, query: 'sa' }, 3, 'Sam')).toEqual({ text: '@Sam you there', cursor: 5 })
  })
})
