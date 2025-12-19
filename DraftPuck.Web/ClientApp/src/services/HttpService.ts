import { parseAllDates } from '@/helpers/dateHelpers'
import axios, { type AxiosInstance, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import TokenService from './TokenService'

export interface IHttpService {
  get<T>(endpoint: string, id?: string): Promise<T>
  getWithParams<T>(endpoint: string, params: {}): Promise<T>
  post<T, R>(endpoint: string, data: T): Promise<R>
  postAsFormData<T, R>(endpoint: string, data: T): Promise<R>
  patch<T, R>(endpoint: string, data: T): Promise<R>
  put<T, R>(endpoint: string, data: T): Promise<R>
  delete(endpoint: string): Promise<void>
}

const apiBasePath = `/api/`
const csrfHeaderKey = 'CSRF-Token'
const refreshTokenUrl = `${apiBasePath}auth/refreshTokens/use`

let refreshPromise: Promise<AuthResponse> | null = null

function isRefreshableError(error: any): boolean {
  const status = error.response?.status
  const url = error.config?.url
  const hasBeenRetried = error.config?._hasBeenRetried

  return status === 401 && url !== refreshTokenUrl && !hasBeenRetried
}

async function refreshTokens(axiosInstance: AxiosInstance): Promise<AuthResponse> {
  if (refreshPromise) return refreshPromise

  refreshPromise = axiosInstance
    .post<any, { data: AuthResponse }>(refreshTokenUrl)
    .then((response) => {
      TokenService.storeTokens(response.data)
      return response.data
    })
    .finally(() => {
      refreshPromise = null
    })

  return refreshPromise
}

function handleRefreshFailure(error: any): never {
  TokenService.clearTokens()

  if (window.location.pathname !== '/login')
    window.location.href = '/login'
  
  throw error
}

export default class HttpService implements IHttpService {
  private _axios: AxiosInstance
  private _basePath: string

  constructor(controller: string) {
    this._axios = axios.create()
    this._basePath = `${apiBasePath}${controller}/`

    this._axios.interceptors.request.use(
      (config) => this.setHeaders(config),
      (error) => Promise.reject(error)
    )

    this._axios.interceptors.response.use(
      (response) => this.handleResponse(response),
      (error) => this.handleResponseError(error)
    )
  }

  private handleResponse(response: AxiosResponse): AxiosResponse {
    parseAllDates(response.data)
    return response
  }

  private async handleResponseError(error: any): Promise<any> {
    if (!isRefreshableError(error)) {
      return Promise.reject(error)
    }

    error.config._hasBeenRetried = true

    try {
      await refreshTokens(this._axios)
      return this._axios(error.config)
    } catch (refreshError) {
      handleRefreshFailure(refreshError)
    }
  }

  private setHeaders(config: InternalAxiosRequestConfig): InternalAxiosRequestConfig {
    config.headers['Accept'] = 'application/json'
    config.headers['Content-Type'] = 'application/json'

    const { jwt, csrf } = TokenService.getTokens()

    if (jwt && config.url !== refreshTokenUrl) {
      config.headers['Authorization'] = `Bearer ${jwt}`
    }

    if (csrf) {
      config.headers[csrfHeaderKey] = csrf
    }

    return config
  }

  public async get<T>(endpoint: string, id?: string): Promise<T> {
    if (endpoint && endpoint.length && !endpoint.includes('?')) endpoint += '/'
    const rel = id && id.length > 0 ? endpoint + id : endpoint
    const response = await this._axios.get<T>(`${this._basePath}${rel}`)
    return response.data
  }

  public async getWithParams<T>(endpoint: string, params: {}): Promise<T> {
    const queryString = new URLSearchParams(params).toString()
    const response = await this._axios.get<T>(`${this._basePath}${endpoint}?${queryString}`)
    return response.data
  }

  public async post<T, R>(endpoint: string, data: T): Promise<R> {
    const response = await this._axios.post<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, data)
    return response.data
  }

  public async postAsFormData<T, R>(endpoint: string, formData: T): Promise<R> {
    const response = await this._axios.post<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    return response.data
  }

  public async patch<T, R>(endpoint: string, data?: T): Promise<R> {
    const response = await this._axios.patch<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, data)
    return response.data
  }

  public async put<T, R>(endpoint: string, data: T): Promise<R> {
    const response = await this._axios.put<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, data)
    return response.data
  }

  public async delete(endpoint: string): Promise<void> {
    await this._axios.delete(`${this._basePath}${endpoint}`)
  }
}
