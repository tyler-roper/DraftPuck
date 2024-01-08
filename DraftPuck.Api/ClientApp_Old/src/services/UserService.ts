import HttpService, { IHttpService } from '@/services/HttpService';

const controller = "user";

class UserService {
    private readonly _http: IHttpService;

    constructor() {
        this._http = new HttpService(controller);
    }

    public async createUser(): Promise<User> {
        return this._http.post("",null);
    }

    public async getUserById(id: string): Promise<User> {
        return this._http.get("", id);
    }
}

export default new UserService();