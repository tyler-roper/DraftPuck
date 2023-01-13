import axios, { AxiosInstance, AxiosResponse } from 'axios';
import parseISO from 'date-fns/parseISO';

const isoDateFormat = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d*)?(?:[-+]\d{2}:?\d{2}|Z)?$/;

function isIsoDateString(value: object): boolean {
    return value && typeof value === "string" && isoDateFormat.test(value);
}

export function handleDates(body: object) {
    if (body === null || body === undefined || typeof body !== "object")
        return body;

    for (const key of Object.keys(body)) {
        let value = body[key];
        if (isIsoDateString(value)) {
            if (!value.endsWith("Z")) value += "Z";
            body[key] = parseISO(value);
        }
        else if (typeof value === "object") handleDates(value);
    }
}

export interface IHttpService {
    get<T>(endpoint: string, id?: string): Promise<T>;
    getWithParams<T>(endpoint: string, params: {}): Promise<T>;
    post<T, R>(endpoint: string, data: T): Promise<R>;
    postAsFormData<T, R>(endpoint: string, data: T): Promise<R>;
    patch<T, R>(endpoint: string, data: T): Promise<R>;
    put<T, R>(endpoint: string, data: T): Promise<R>;
    delete(endpoint: string): Promise<void>;
    eventSource(endpoint: string): EventSource;
}

const apiBasePath = `/api/`;

export default class HttpService implements IHttpService {

    private _axios: AxiosInstance;
    private _basePath: string;

    constructor(controller: string, addUserHeader = true, basePath?: string) {
        const axiosInstance = axios.create();
        axiosInstance.interceptors.request.use(config => {
            const userId = localStorage.getItem('userId');
            config.headers['Accept'] = 'application/json';
            config.headers['Content-Type'] = 'application/json';

            if (userId && addUserHeader)
                config.headers["user-id"] = userId;
            return config;
        }, error => {
            Promise.reject(error);
        });

        axiosInstance.interceptors.response.use(originalResponse => {
            handleDates(originalResponse.data);
            return originalResponse;
        });

        this._axios = axiosInstance;
        this._basePath = basePath ? `${basePath}${controller}` : `${apiBasePath}${controller}/`;
    }

    public async get<T>(endpoint: string, id?: string): Promise<T> {
        if (endpoint && endpoint.length && !endpoint.includes("?")) endpoint += "/";
        const rel = id && id.length > 0 ? endpoint + id : endpoint;
        const response = await this._axios.get<T>(`${this._basePath}${rel}`);
        return response.data;
    }

    public eventSource(endpoint: string): EventSource {
        return new EventSource(`${this._basePath}${endpoint}`);
    }

    public async getWithParams<T>(endpoint: string, params: {}): Promise<T> {
        const queryString = new URLSearchParams(params).toString();
        const response = await this._axios.get<T>(`${this._basePath}${endpoint}?${queryString}`);
        return response.data;
    }

    public async post<T, R>(endpoint: string, data: T): Promise<R> {
        const response = await this._axios.post<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, data);
        return response.data;
    }

    public async postAsFormData<T, R>(endpoint: string, formData: T): Promise<R> {
        const response = await this._axios.post<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        return response.data;
    }

    public async patch<T, R>(endpoint: string, data?: T): Promise<R> {
        if (typeof data !== 'undefined') {
            const response = await this._axios.patch<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, data);
            return response.data;
        }

        const response = await this._axios.patch<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, data);
        return response.data;
    }

    public async put<T, R>(endpoint: string, data: T): Promise<R> {
        const response = await this._axios.put<T, AxiosResponse<R>>(`${this._basePath}${endpoint}`, data);
        return response.data;
    }

    public async delete(endpoint: string): Promise<void> {
        await this._axios.delete(`${this._basePath}${endpoint}`);
    }
}