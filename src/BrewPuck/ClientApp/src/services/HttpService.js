import axios from 'axios';
const apiBasePath = `https://statsapi.web.nhl.com/api/v1`;
export default class HttpService {
    constructor(controller) {
        const axiosInstance = axios.create();
        this._axios = axiosInstance;
        this._basePath = `${apiBasePath}${controller}/`;
    }
    async get(endpoint, id) {
        if (endpoint && endpoint.length && !endpoint.includes("?"))
            endpoint += "/";
        const rel = id && id.length > 0 ? endpoint + id : endpoint;
        const response = await this._axios.get(`${this._basePath}${rel}`);
        return response.data;
    }
    async getWithParams(endpoint, params) {
        const queryString = new URLSearchParams(params).toString();
        const response = await this._axios.get(`${this._basePath}${endpoint}/?${queryString}`);
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