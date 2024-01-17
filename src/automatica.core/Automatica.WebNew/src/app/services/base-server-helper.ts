import { environment } from "src/environments/environment";

export class BaseServiceHelper {
    public static getApiBaseUrl(): string {
        let s1Server = localStorage.getItem("s1server");

        if (!s1Server) {
            s1Server = environment.s1server;
        }

        return s1Server + "/webapi";
    }
    public static getSignalRBaseUrl(): string {
        return this.getBaseUrl();
    }
    public static getBaseUrl(): string {
        let s1Server = localStorage.getItem("s1server");

        if (!s1Server) {
            s1Server = environment.s1server;
        }

        return s1Server;
    }
}