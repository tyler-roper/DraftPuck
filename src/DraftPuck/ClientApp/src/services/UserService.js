import HttpService from '@/services/HttpService';
const controller = "user";
class UserService {
    constructor() {
        this._http = new HttpService(controller);
    }
    async createUser() {
        return this._http.post("", null);
    }
    async getUserById(id) {
        return this._http.get("", id);
    }
}
export default new UserService();
//# sourceMappingURL=UserService.js.map