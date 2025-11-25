interface UpdateUserRequest {
  targetUserId?: string
  requesterUserId?: string
  email?: string
  nickname?: string
  fcmRegistrationToken?: string
  drinkReceivedNotificationPreference?: NotificationPreference
  drinkAwardedNotificationPreference?: NotificationPreference
  chatNotificationPreference?: NotificationPreference
  pickingStartedNotificationPreference?: NotificationPreference
  achievementAwardedNotificationPreference?: NotificationPreference
  bannerId?: string
  titleId?: string
  password?: string
  confirmPassword?: string
  avatarData?: string
}
