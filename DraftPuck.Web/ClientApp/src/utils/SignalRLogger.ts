import type { ILogger, LogLevel } from '@microsoft/signalr'

export class SignalRLogger implements ILogger {
  logLevel = 0

  constructor(_logLevel: number) {
    this.logLevel = _logLevel
  }

  log(_: LogLevel, message: string) {
    // This will be provided by the component using this logger
    if (this.onLog) {
      this.onLog(message, this.logLevel)
    }
  }

  onLog?: (message: string, level: number) => void
}
