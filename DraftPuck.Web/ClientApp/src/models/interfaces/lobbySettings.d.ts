import type Bot from '@/models/Bot'

interface LobbySettings {
  picksPerTeam: number
  bots: Bot[]
  isBotAutoPickingEnabled: boolean
  gameIds: number[]
}
