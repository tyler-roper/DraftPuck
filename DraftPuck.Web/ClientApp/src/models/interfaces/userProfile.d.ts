/** Another player's public profile (GET /api/users?name=): what their profile page shows, nothing private. */
interface UserProfile {
  id: string
  nickname?: string
  isGuest: boolean
  isBot: boolean
  created: Date
  avatarPath?: string
  banner?: Banner
  title?: Title
  achievements: Array<UserAchievement>
}
