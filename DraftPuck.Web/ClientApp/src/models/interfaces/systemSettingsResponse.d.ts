interface SystemSettingsResponse {
  isTestMode: boolean
  startupTimeUtc: Date
  testModeStartDateTimeUtc: Date
  gitSha: string
  // The logos to use instead of the usual ones while an event is on.
  eventLogo: { name: string; logoUrl: string; wideLogoUrl: string } | null
}
