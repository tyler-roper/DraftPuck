import HttpService, { type IHttpService } from '@/services/HttpService'
import type { AchievementBackfill } from '@/helpers/backfillHelpers'

const controller = 'admin/achievements'

class AdminAchievementService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  /** Starts a backfill: a silent check of every achievement for everyone who has played. */
  public async startBackfill(): Promise<AchievementBackfill> {
    return await this._http.post('backfill', {})
  }

  /** The latest backfill run, or null if there's never been one (the API answers 204). */
  public async getLatestBackfill(): Promise<AchievementBackfill | null> {
    const run = await this._http.get<AchievementBackfill | ''>('backfill/latest')
    return run || null
  }
}

export default new AdminAchievementService()
