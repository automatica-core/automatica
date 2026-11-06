import { firstValueFrom } from "rxjs";
import { L10nTranslationService } from "angular-l10n";
import { Router } from "@angular/router";
import { environment } from "../../environments/environment";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { WebApiException, ExceptionSeverity } from "../base/model/web-api-exception";
import { BaseModel } from "../base/model/base-model";
import { BaseServiceHelper } from "./base-server-helper";

/**
 * Base for all web api services. The auth header, the request timeout and the 401 handling live in apiInterceptor.
 */
export class BaseService {
    public static getValidBaseModels<T extends BaseModel>(jsonArr: any, translationService: L10nTranslationService) {
        const list: Array<T> = [];
        for (const node of jsonArr) {
            const model = BaseModel.getBaseModelFromJson<T>(node, void 0, translationService);
            if (model) {
                list.push(model);
            } else {
                if (environment.errorLogging) {
                    console.error("BaseModel.getBaseModelFromJson returned a null object for", node);
                }
            }
        }
        return list;
    }

    constructor(private httpService: HttpClient, private pRouter: Router, protected translationService: L10nTranslationService) {

    }

    private headers(): HttpHeaders {
        return new HttpHeaders({
            "Content-Type": "application/json",
            "Accept": "application/json"
        });
    }

    protected getS1Server() {
        return BaseServiceHelper.getApiBaseUrl();
    }

    private async requestJson(method: string, url: string, body?: any, withCredentials: boolean = true): Promise<any> {
        try {
            return await firstValueFrom(this.httpService.request(method, this.getS1Server() + "/" + url,
                { body: body, headers: this.headers(), withCredentials: withCredentials }));
        } catch (error) {
            throw this.handleError(error);
        }
    }

    private toModel<T extends BaseModel>(json: any): T {
        if (!json) {
            return void 0;
        }
        return BaseModel.getBaseModelFromJson<T>(json, void 0, this.translationService);
    }

    private toModels<T extends BaseModel>(json: any): T[] {
        if (!json) {
            return void 0;
        }
        if (!(json instanceof Array)) {
            console.error("array expected at response: ", json);
            throw new Error("array expected at response");
        }
        return BaseService.getValidBaseModels<T>(json, this.translationService);
    }

    async get<T extends BaseModel>(url: string): Promise<T> {
        return this.toModel<T>(await this.requestJson("GET", url));
    }

    async getMultiple<T extends BaseModel>(url: string): Promise<Array<T>> {
        return this.toModels<T>(await this.requestJson("GET", url));
    }

    async getJson(url: string): Promise<any> {
        const json = await this.requestJson("GET", url);
        return json ? json : void 0;
    }

    async getRaw(url: string): Promise<any> {
        try {
            const data = await firstValueFrom(this.httpService.get(this.getS1Server() + "/" + url,
                { headers: this.headers(), withCredentials: true, responseType: "text" }));

            return data ? data : void 0;
        } catch (error) {
            throw this.handleError(error);
        }
    }

    async postRaw(url: string, body: any, withCredentials: boolean = true): Promise<any> {
        const json = await this.requestJson("POST", url, body, withCredentials);
        return json ? json : void 0;
    }

    async post<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T> {
        return this.toModel<T>(await this.requestJson("POST", url, body, withCredentials));
    }

    async postMultiple<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T[]> {
        return this.toModels<T>(await this.requestJson("POST", url, body, withCredentials));
    }

    async postJson(url: string, body: any, withCredentials: boolean = true): Promise<any> {
        return await this.requestJson("POST", url, body, withCredentials);
    }

    async put<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T> {
        return this.toModel<T>(await this.requestJson("PUT", url, body, withCredentials));
    }

    async putJson(url: string, body: any, withCredentials: boolean = true): Promise<any> {
        return await this.requestJson("PUT", url, body, withCredentials);
    }

    async patch<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T> {
        return this.toModel<T>(await this.requestJson("PATCH", url, body, withCredentials));
    }

    async patchJson(url: string, body: any, withCredentials: boolean = true): Promise<any> {
        const json = await this.requestJson("PATCH", url, body, withCredentials);
        return json ? json : void 0;
    }

    async deleteJson(url: string, withCredentials: boolean = true): Promise<any> {
        return await this.requestJson("DELETE", url, void 0, withCredentials);
    }

    private handleError = (error: any) => {
        if (error?.error instanceof ArrayBuffer) {
            const arrayBuf: ArrayBuffer = error.error;
            if (arrayBuf.byteLength > 0) {
                const decodedString = String.fromCharCode.apply(null, new Uint8Array(arrayBuf));
                error.error = JSON.parse(decodedString);
            }
        }

        const status: number = error?.status;

        if (status === 401) {
            // apiInterceptor already dropped the token and navigated to the login (except for the login request)
            const ex = new WebApiException();
            ex.Severity = ExceptionSeverity.Error;
            ex.ErrorText = "UNAUTHORISED";
            ex.status = status;
            return ex;
        } else if (error?.error && (error.error.hasOwnProperty("TypeInfo") || error.error.hasOwnProperty("typeInfo"))) {
            const ex = new WebApiException();
            BaseModel.fromJson(error.error, ex, this.translationService);
            ex.status = status;
            return ex;
        }
        return error;
    }

}
