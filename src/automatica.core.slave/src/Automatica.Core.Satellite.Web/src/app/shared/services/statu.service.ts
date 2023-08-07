import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { BaseService } from "./base.service";

@Injectable()
export class StatusService extends BaseService {
    constructor(http: HttpClient) {
        super(http);
    }

    public async getStatus(): Promise<any> {
        const result = await this.http.get("/webapi/status", { headers: this.headers() }).toPromise();
        return result;
    }
}