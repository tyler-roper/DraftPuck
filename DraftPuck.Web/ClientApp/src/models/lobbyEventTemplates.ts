export default class LobbyEventTemplate {
  tokens: string[]
  replaceFunction: (templateString: string, eventText: string) => string

  constructor(tokens: string[], replaceFunction: (templateString: string, eventText: string) => string) {
    this.tokens = tokens
    this.replaceFunction = replaceFunction
  }

  replaceTokens(templatedString: string) {
    return this.tokens.reduce((output, token) => {
      if (templatedString.includes(token)) return this.replaceFunction(token, output)
      else return output
    }, templatedString)
  }
}
