import { throwError as observableThrowError, Observable } from "rxjs";
import { TranslationService } from "angular-l10n";
import { Router } from "@angular/router";
import { environment } from "../../environments/environment";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { WebApiException, ExceptionSeverity } from "../base/model/web-api-exception";
import * as msgpack from "msgpack-lite";
import { BaseModel } from "../base/model/base-model";

export class BaseService {
    public static getValidBaseModels<T extends BaseModel>(jsonArr: any, translationService: TranslationService) {
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

    constructor(private httpService: HttpClient, private pRouter: Router, protected translationService: TranslationService) {

    }



    private headers(): HttpHeaders {
        let headers = new HttpHeaders();
        headers = headers.append("Content-Type", "application/x-msgpack");
        headers = headers.append("Accept", "application/x-msgpack");
        headers = headers.append("Access-Control-Allow-Origin", "*");

        const jwt = localStorage.getItem("jwt");

        if (jwt) {
            headers = headers.append("Authorization", "Bearer " + jwt);
        }

        return headers;
    }

    private getS1Server() {
        let s1Server = localStorage.getItem("s1server");

        if (!s1Server) {
            s1Server = environment.s1server;
        }

        return s1Server + "/webapi";
    }

    async get<T extends BaseModel>(url: string): Promise<T> {
        try {
            const data = await this.httpService.get(this.getS1Server() + "/" + url,
                { headers: this.headers(), withCredentials: true, responseType: "arraybuffer" }).toPromise();

            if (!data) {
                return void 0;
            }

            const json = this.decode(data);
            if (json == null) {
                return;
            }
            const model = BaseModel.getBaseModelFromJson<T>(json, void 0, this.translationService);
            return model;

        } catch (error) {
            throw this.handleError(error);
        }
    }


    async getAbsoluteJson(url: string): Promise<any> {
        const data = await this.httpService.get(this.getS1Server() + "/" + url,
            { headers: this.headers(), withCredentials: true, responseType: "arraybuffer" }).toPromise();
        if (!data) {
            return void 0;
        }

        const json = this.decode(data);
        return json;
    }


    async getMultiple<T extends BaseModel>(url: string): Promise<Array<T>> {
        try {
            const data = await this.httpService.get(this.getS1Server() + "/" + url,
                { headers: this.headers(), withCredentials: true, responseType: "arraybuffer" }).toPromise();
            if (!data) {
                return void 0;
            }

            const json = this.decode(data);
            if (!(json instanceof Array)) {
                console.error("array expected at response: ", data);
                observableThrowError("array expected at response");
            }

            const v = BaseService.getValidBaseModels<T>(json, this.translationService);
            return v;

        } catch (error) {
            throw this.handleError(error);
        }
    }


    async getJson(url: string): Promise<any> {
        try {
            const data = await this.httpService.get(this.getS1Server() + "/" + url,
                { headers: this.headers(), withCredentials: true, responseType: "arraybuffer" }).toPromise();

            if (!data) {
                return void 0;
            }
            const json = this.decode(data);
            return json;

        } catch (error) {
            throw this.handleError(error);
        }
    }

    private decode(data: ArrayBuffer) {
        const json = msgpack.decode(new Uint8Array(data));
        return json;
    }

    private buf2hex(buffer) {
        return Array.prototype.map.call(buffer, x => ("00" + x.toString(16)).slice(-2)).join("");
    }
    private encode(data: any): any {
        return new Uint8Array(msgpack.encode(data)).buffer;
    }

    async post<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T> {
        try {
            const data = this.encode(body);
            const response = await this.httpService.post(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers(), responseType: "arraybuffer" }).toPromise();

            if (!response) {
                return void 0;
            }

            const json = this.decode(response);
            return BaseModel.getBaseModelFromJson<T>(json, void 0, this.translationService);

        } catch (error) {
            throw this.handleError(error);
        }
    }
    async postMultiple<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T[]> {
        try {
            const data = this.encode(body);
            const response = await this.httpService.post(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers(), responseType: "arraybuffer" }).toPromise();

            if (!data) {
                return void 0;
            }

            const jsonArr = this.decode(response);

            if (!(jsonArr instanceof Array)) {
                console.error("array expected at response: ", response);
                observableThrowError("array expected at response");
            }

            return BaseService.getValidBaseModels<T>(jsonArr, this.translationService);

        } catch (error) {
            throw this.handleError(error);
        }
    }

    async postJson(url: string, body: any, withCredentials: boolean = true): Promise<any> {
        try {
            const data = this.encode(body);
            const response = await this.httpService.post(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers(), responseType: "arraybuffer" }).toPromise();

            if (!data) {
                return void 0;
            }

            const json = this.decode(response);
            return json;

        } catch (error) {
            throw this.handleError(error);
        }
    }

    // async patch<T extends BaseModel>(url: string, body: any): Promise<T> {
    //     try {
    //         const response = await this.httpService.patch(this.getS1Server() + "/" + url, body,
    //             { withCredentials: true, headers: this.headers(), observe: "response" }).toPromise();
    //         return response.body as T;
    //     } catch (error) {
    //         throw this.handleError(error);
    //     }
    // }

    // async put<T extends BaseModel>(url: string, body: any): Promise<T> {
    //     try {
    //         const response = await this.httpService.post(this.getS1Server() + "/" + url, body,
    //             { withCredentials: true, headers: this.headers(), observe: "response" }).toPromise();
    //         return BaseModel.getBaseModelFromJson<T>(response.body, void 0, this.translationService);
    //     } catch (error) {
    //         throw this.handleError(error);
    //     }
    // }

    // async delete(url: string): Promise<any> {
    //     try {
    //         const response = await this.httpService.patch(this.getS1Server() + "/" + url,
    //             { withCredentials: true, headers: this.headers() }).toPromise();
    //         return response;
    //     } catch (error) {
    //         throw this.handleError(error);
    //     }
    // }


    private handleError = (error: any) => {
        if (error.error instanceof ArrayBuffer) {
            const arrayBuf: ArrayBuffer = error.error;
            if (arrayBuf.byteLength > 0) {
                const decodedString = String.fromCharCode.apply(null, new Uint8Array(arrayBuf));
                error.error = JSON.parse(decodedString);
            }
        }

        if (error.status === 401) {
            if (localStorage.getItem("jwt")) {
                window.location.href = "/login";
                return;
            } else {
                this.pRouter.navigate(["/login"]);
            }

            localStorage.removeItem("jwt");
            const ex = new WebApiException();
            ex.Severity = ExceptionSeverity.Error;
            ex.ErrorText = "UNAUTHORISED";
            return ex;
        } else if (error.error && error.error.hasOwnProperty("TypeInfo")) {
            const ex = new WebApiException();
            BaseModel.fromJson(error.error, ex, this.translationService);
            return ex;
        }
    }

}
