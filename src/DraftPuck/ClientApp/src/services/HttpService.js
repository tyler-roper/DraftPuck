import axios from 'axios';
import parseISO from 'date-fns/parseISO';
const isoDateFormat = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d*)?(?:[-+]\d{2}:?\d{2}|Z)?$/;
function isIsoDateString(value) {
    return value && typeof value === "string" && isoDateFormat.test(value);
}
export function handleDates(body) {
    if (body === null || body === undefined || typeof body !== "object")
        return body;
    for (const key of Object.keys(body)) {
        let value = body[key];
        if (isIsoDateString(value)) {
            if (!value.endsWith("Z"))
                value += "Z";
            body[key] = parseISO(value);
        }
        else if (typeof value === "object")
            handleDates(value);
    }
}
const apiBasePath = `/api/`;
export default class HttpService {
    constructor(controller, addUserHeader = true, basePath) {
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
    async get(endpoint, id) {
        if (endpoint && endpoint.length && !endpoint.includes("?"))
            endpoint += "/";
        const rel = id && id.length > 0 ? endpoint + id : endpoint;
        const response = await this._axios.get(`${this._basePath}${rel}`);
        return response.data;
    }
    eventSource(endpoint) {
        return new EventSource(`${this._basePath}${endpoint}`);
    }
    async getWithParams(endpoint, params) {
        const queryString = new URLSearchParams(params).toString();
        const response = await this._axios.get(`${this._basePath}${endpoint}?${queryString}`);
        return response.data;
    }
    async post(endpoint, data) {
        const response = await this._axios.post(`${this._basePath}${endpoint}`, data);
        return response.data;
    }
    async postAsFormData(endpoint, formData) {
        const response = await this._axios.post(`${this._basePath}${endpoint}`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        return response.data;
    }
    async patch(endpoint, data) {
        if (typeof data !== 'undefined') {
            const response = await this._axios.patch(`${this._basePath}${endpoint}`, data);
            return response.data;
        }
        const response = await this._axios.patch(`${this._basePath}${endpoint}`, data);
        return response.data;
    }
    async put(endpoint, data) {
        const response = await this._axios.put(`${this._basePath}${endpoint}`, data);
        return response.data;
    }
    async delete(endpoint) {
        await this._axios.delete(`${this._basePath}${endpoint}`);
    }
}
//# sourceMappingURL=HttpService.js.map