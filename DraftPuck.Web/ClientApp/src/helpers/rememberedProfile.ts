/**
 * The signed-in user's display profile (name, avatar, banner, title), kept on the device so the next visit can show it
 * straight away while the session is restored. No tokens, nothing private: just what the profile card shows.
 */
export type RememberedProfile = Pick<User, 'id' | 'nickname' | 'avatarPath' | 'banner' | 'title' | 'isGuest' | 'isAdmin'>

const KEY = 'draftpuck:profile'

/** Remembers a signed-in user; forgets the profile for a guest (or no one). */
export function rememberProfile(user: User | undefined, storage: Storage | undefined = safeStorage()) {
  try {
    if (!user || user.isGuest !== false) return storage?.removeItem(KEY)
    const profile: RememberedProfile = {
      id: user.id,
      nickname: user.nickname,
      avatarPath: user.avatarPath,
      banner: user.banner,
      title: user.title,
      isGuest: false,
      isAdmin: user.isAdmin
    }
    storage?.setItem(KEY, JSON.stringify(profile))
  } catch {
    // Storage full or blocked: the page just waits for the session, as before.
  }
}

export function rememberedProfile(storage: Storage | undefined = safeStorage()): RememberedProfile | undefined {
  try {
    const profile = JSON.parse(storage?.getItem(KEY) ?? 'null') as RememberedProfile | null
    return profile?.id && profile.nickname && profile.isGuest === false ? profile : undefined
  } catch {
    return undefined
  }
}

function safeStorage(): Storage | undefined {
  try {
    return window.localStorage
  } catch {
    return undefined
  }
}
