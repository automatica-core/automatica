import { Injectable } from "@angular/core";
import { BaseService } from "../shared/services/base.service";
import { HttpClient } from "@angular/common/http";

export interface Config {

}



@Injectable()
export class ConfigService extends BaseService {

    constructor(http: HttpClient) {
        super(http);
    }

    async getConfig(): Promise<Config> {
        const result = <Config>await this.http.get("/webapi/config", { headers: this.headers() }).toPromise();


        return result;
    }

    async saveConfig(config: Config) {
         <Config>await this.http.post("/webapi/config", config, { headers: this.headers() }).toPromise();
        return this.getConfig();
    }

}