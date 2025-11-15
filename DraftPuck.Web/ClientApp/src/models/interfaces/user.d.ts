interface User {
  id: string
  email?: string
  isGuest: boolean
  isAdmin: boolean
  isActive: boolean
  nickname?: string
  created: Date
  isBot: boolean
  banner: Banner
  title: Title
  ownedBanners: Array<Banner>
  ownedTitles: Array<Title>
  achievements: Array<UserAchievement>
  avatarPath?: string
  fcmRegistrationToken?: string
  drinkReceivedNotificationPreference: NotificationPreference
  drinkAwardedNotificationPreference: NotificationPreference
  chatNotificationPreference: NotificationPreference
  pickingStartedNotificationPreference: NotificationPreference
}
