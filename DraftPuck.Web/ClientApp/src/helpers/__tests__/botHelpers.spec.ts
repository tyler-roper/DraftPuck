import { describe, expect, it } from 'vitest'
import { BOT_PICK_STYLE_OPTIONS, newBot } from '@/helpers/botHelpers'
import BotPickStyle from '@/enums/botPickStyle'
import BotNames from '@/models/botNames'

describe('newBot', () => {
  it('picks at random', () => {
    expect(newBot([]).pickStyle).toBe(BotPickStyle.Random)
  })

  it('is named from the bot names, never one another bot has', () => {
    const [first, second] = BotNames
    const offered: string[][] = []

    const bot = newBot([{ nickname: first, pickStyle: 0 }], (names) => {
      offered.push(names)
      return names[0]
    })

    expect(offered[0]).not.toContain(first)
    expect(bot.nickname).toBe(second)
  })

  it('falls back to a numbered name once every name is taken', () => {
    const bots = BotNames.map((nickname) => ({ nickname, pickStyle: 0 }))

    expect(newBot(bots).nickname).toBe(`Bot ${bots.length}`)
  })
})

describe('BOT_PICK_STYLE_OPTIONS', () => {
  it('lists every strategy by name, best first', () => {
    expect(BOT_PICK_STYLE_OPTIONS).toEqual([
      { text: 'Best', value: BotPickStyle.Best },
      { text: 'Good', value: BotPickStyle.Good },
      { text: 'Average', value: BotPickStyle.Average },
      { text: 'Bad', value: BotPickStyle.Bad },
      { text: 'Worst', value: BotPickStyle.Worst },
      { text: 'Random', value: BotPickStyle.Random }
    ])
  })
})
