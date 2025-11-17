export default class SystemMessageViewModel {
  message: string
  sent: Date
  isSystem: boolean = true

  constructor(message: string) {
    this.message = message
    this.sent = new Date()
  }
}
