interface Banner {
  id: string
  imagePath: string
  uniqueIdentifier: string
  /** The achievement that awards it; null for the defaults every player has. */
  achievementId: string | null
}
