/** What chat commands can see and do. Their output goes to the chat as system messages. */
export interface ChatCommandContext {
  lobby: Lobby | undefined
  currentMember: LobbyMember | undefined
  setDebugging: (level: number) => void
  sendSystemMessage: (text: string) => void
  describeConnection: () => object
}

const json = (value: unknown) => JSON.stringify(value, undefined, 2)

/** A member's details without their (long) messages and picks. */
function memberInfo(member: LobbyMember): Partial<LobbyMember> {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { messages, picks, ...info } = member
  return info
}

const commands: Record<string, (ctx: ChatCommandContext, args: string[]) => void> = {
  /** /debug [level]: debug output in the chat, level 1-3 (default 1); 0 turns it off. */
  debug(ctx, [level]) {
    const requested = level === undefined || isNaN(+level) ? 1 : +level
    const clamped = Math.min(3, requested)
    ctx.setDebugging(clamped)
    ctx.sendSystemMessage(clamped === 0 ? 'Debugging disabled.' : `Debugging enabled (Level ${clamped}).`)
  },
  /** /connection: the live connection's ID, URL and state. */
  connection(ctx) {
    ctx.sendSystemMessage(json(ctx.describeConnection()))
  },
  /** /me: your lobby member details. */
  me(ctx) {
    if (ctx.currentMember) ctx.sendSystemMessage(json(memberInfo(ctx.currentMember)))
  },
  /** /user <name>: another member's details, by name (any case, spaces allowed). */
  user(ctx, nameParts) {
    const name = nameParts.join(' ')
    const member = ctx.lobby?.members.find((m) => m.name.toUpperCase() === name.toUpperCase())
    ctx.sendSystemMessage(member ? json(memberInfo(member)) : `User ${name} not found.`)
  }
}

/** Runs a chat command (typed as "/command args..."). Returns false for an unknown command. */
export function runChatCommand(command: string, args: string[], ctx: ChatCommandContext): boolean {
  const run = commands[command]
  if (!run) return false
  run(ctx, args)
  return true
}
