interface AuthResponse {
  user: User
  jwtToken: string
  refreshToken: string
  antiCsrfToken: string
}
