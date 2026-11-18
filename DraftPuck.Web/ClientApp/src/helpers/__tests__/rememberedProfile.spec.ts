import { describe, expect, it } from 'vitest'
import { rememberProfile, rememberedProfile } from '@/helpers/rememberedProfile'

function memoryStorage(): Storage {
  const items = new Map<string, string>()
  return {
    get length() {
      return items.size
    },
    clear: () => items.clear(),
    getItem: (k) => items.get(k) ?? null,
    key: (i) => [...items.keys()][i] ?? null,
    removeItem: (k) => void items.delete(k),
    setItem: (k, v) => void items.set(k, v)
  }
}

const signedIn = {
  id: 'u1',
  nickname: 'tyler',
  isGuest: false,
  isAdmin: true,
  avatarPath: '/a.png',
  banner: { id: 'b', imagePath: '/b.png' },
  title: { id: 't', text: 'Rookie' },
  achievements: [{ id: 'x' }]
} as unknown as User

describe('rememberedProfile', () => {
  it('keeps only what the profile card shows', () => {
    const storage = memoryStorage()
    rememberProfile(signedIn, storage)
    expect(rememberedProfile(storage)).toEqual({
      id: 'u1',
      nickname: 'tyler',
      isGuest: false,
      isAdmin: true,
      avatarPath: '/a.png',
      banner: { id: 'b', imagePath: '/b.png' },
      title: { id: 't', text: 'Rookie' }
    })
  })

  it('forgets the profile for a guest, or once signed out', () => {
    const storage = memoryStorage()
    rememberProfile(signedIn, storage)
    rememberProfile({ id: 'g', isGuest: true } as User, storage)
    expect(rememberedProfile(storage)).toBeUndefined()

    rememberProfile(signedIn, storage)
    rememberProfile(undefined, storage)
    expect(rememberedProfile(storage)).toBeUndefined()
  })

  it('ignores anything unreadable', () => {
    const storage = memoryStorage()
    storage.setItem('draftpuck:profile', '{not json')
    expect(rememberedProfile(storage)).toBeUndefined()
    expect(rememberedProfile(undefined)).toBeUndefined()
  })
})
