interface User {
  id: string
  created: Date
  isBot: boolean
  fcmRegistrationToken?: string
  drinkReceivedNotificationPreference: NotificationPreference
  drinkAwardedNotificationPreference: NotificationPreference
  chatNotificationPreference: NotificationPreference
  pickingStartedNotificationPreference: NotificationPreference
}
