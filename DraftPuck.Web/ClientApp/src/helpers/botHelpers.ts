import BotPickStyle from '@/enums/botPickStyle'
import BotNames from '@/models/botNames'
import '@/extensions/arrayExtensions'

/** The most bots a lobby can start with (the server checks it too). */
export const MAX_BOTS = 5

/** The picking strategies, in order, as the create form lists them. */
export const BOT_PICK_STYLE_OPTIONS: { text: string; value: BotPickStyle }[] = Object.entries(BotPickStyle)
  .filter((entry): entry is [string, BotPickStyle] => typeof entry[1] === 'number')
  .map(([text, value]) => ({ text, value }))

/** A bot to add: a name none of the others has (chosen at random), picking at random. */
export function newBot(bots: Bot[], choose: (names: string[]) => string = (names) => names.random()): Bot {
  const taken = new Set(bots.map((bot) => bot.nickname))
  const unused = BotNames.filter((name) => !taken.has(name))
  return { nickname: unused.length ? choose(unused) : `Bot ${bots.length}`, pickStyle: BotPickStyle.Random }
}
