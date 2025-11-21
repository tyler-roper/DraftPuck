class TokenService {
  private JWT_KEY = 'jwt'
  private CSRF_KEY = 'csrf-token'

  public storeTokens(authResponse: AuthResponse) {
    localStorage.setItem(this.JWT_KEY, authResponse.jwtToken)
    localStorage.setItem(this.CSRF_KEY, authResponse.antiCsrfToken)
  }

  public clearTokens() {
    localStorage.removeItem(this.JWT_KEY)
    localStorage.removeItem(this.CSRF_KEY)
  }

  public getTokens() {
    return {
      jwt: localStorage.getItem(this.JWT_KEY),
      csrf: localStorage.getItem(this.CSRF_KEY)
    }
  }
}

export default new TokenService()
