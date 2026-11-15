/** How a banner or title became yours, for the back of its card. */
export type HowEarned = { kind: 'default' } | { kind: 'achievement'; name: string; description: string; dateEarned?: Date }

/**
 * How the user got a banner or title they own: a default every player has, or the achievement that awarded it (from
 * the user's achievements). Null if the user somehow doesn't have that achievement, so there's nothing to show.
 */
export function howEarned(item: { achievementId: string | null }, achievements: UserAchievement[]): HowEarned | null {
  if (!item.achievementId) return { kind: 'default' }

  const achievement = achievements.find((a) => a.achievementId === item.achievementId)
  if (!achievement) return null

  return { kind: 'achievement', name: achievement.friendlyName, description: achievement.description, dateEarned: achievement.dateEarned }
}
