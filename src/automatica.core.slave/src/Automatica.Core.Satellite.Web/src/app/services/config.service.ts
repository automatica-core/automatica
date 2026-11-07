import { Injectable } from "@angular/core";
import { BaseService } from "../shared/services/base.service";
import { HttpClient } from "@angular/common/http";

export interface Config {
    serverUrl?: string;
    clientId?: string;
    /** write-only: never returned by the api, leave empty to keep the current key */
    clientKey?: string;
    hasClientKey?: boolean;
    port?: number;
    dockerTag?: string;
    /** null = automatic: switch to TLS once the core offers it */
    useTls?: boolean | null;
    tlsPort?: number;
    tlsFingerprint?: string;
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

    /** random 256 bit key, base64url */
    static generateKey(): string {
        const bytes = new Uint8Array(32);
        crypto.getRandomValues(bytes);
        let binary = "";
        bytes.forEach(b => binary += String.fromCharCode(b));
        return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
    }
}
