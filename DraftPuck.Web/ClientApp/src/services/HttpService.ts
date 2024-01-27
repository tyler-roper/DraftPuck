import { parseAllDates } from '@/helpers/dateHelpers'
import axios, { type AxiosInstance, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'

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

export default class HttpService implements IHttpService {
  private _axios: AxiosInstance
  private _basePath: string

  constructor(controller: string) {
    const axiosInstance = axios.create()
    axiosInstance.interceptors.request.use(
      (config) => {
        this.setHeaders(config)
        return config
      },
      (error) => {
        Promise.reject(error)
      }
    )

    axiosInstance.interceptors.response.use((originalResponse) => {
      parseAllDates(originalResponse.data)
      return originalResponse
    })

    this._axios = axiosInstance
    this._basePath = `${apiBasePath}${controller}/`
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
    if (typeof data !== 'undefined') {
      const response = await this._axios.patch<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, data)
      return response.data
    }

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

  setHeaders(config: InternalAxiosRequestConfig) {
    const userId = localStorage.getItem('userId')
    config.headers['Accept'] = 'application/json'
    config.headers['Content-Type'] = 'application/json'

    if (userId) config.headers['user-id'] = userId
  }
}
