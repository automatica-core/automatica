import { throwError as observableThrowError, Observable, timeout } from "rxjs";
import { L10nTranslationService } from "angular-l10n";
import { Router } from "@angular/router";
import { environment } from "../../environments/environment";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { WebApiException, ExceptionSeverity } from "../base/model/web-api-exception";
import * as msgpack from "msgpack-lite";
import { BaseModel } from "../base/model/base-model";
import { BaseServiceHelper } from "./base-server-helper";

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
        let headers = new HttpHeaders();
        headers = headers.append("Content-Type", "application/json");
        headers = headers.append("Accept", "application/json");
        headers = headers.append("Access-Control-Allow-Origin", "*");

        const jwt = localStorage.getItem("jwt");

        if (jwt) {
            headers = headers.append("Authorization", "Bearer " + jwt);
        }

        return headers;
    }

    protected getS1Server() {
     return BaseServiceHelper.getApiBaseUrl();
    }

    async get<T extends BaseModel>(url: string): Promise<T> {
        try {
            const data = await this.httpService.get(this.getS1Server() + "/" + url,
                { headers: this.headers(), withCredentials: true }).toPromise();

            if (!data) {
                return void 0;
            }

            // const json = this.decode(url, data);
            // if (json == null) {
            //     return;
            // }
            const json = data;
            const model = BaseModel.getBaseModelFromJson<T>(json, void 0, this.translationService);
            console.log(url, "data is", model);
            return model;

        } catch (error) {
            throw this.handleError(error);
        }
    }


    async getAbsoluteJson(url: string): Promise<any> {
        const data = await this.httpService.get(this.getS1Server() + "/" + url,
            { headers: this.headers(), withCredentials: true }).toPromise();
        if (!data) {
            return void 0;
        }
        // return data;

        // const json = this.decode(url, data);
        // return json;
    }


    async getMultiple<T extends BaseModel>(url: string): Promise<Array<T>> {
        try {
            const data = await this.httpService.get(this.getS1Server() + "/" + url,
                { headers: this.headers(), withCredentials: true }).toPromise();
            if (!data) {
                return void 0;
            }

            const json = data; // this.decode(url, data);
            if (!(json instanceof Array)) {
                console.error("array expected at response: ", data);
                observableThrowError("array expected at response");
            }

            const v = BaseService.getValidBaseModels<T>(json, this.translationService);
            console.log(url, "data is", v);
            return v;

        } catch (error) {
            throw this.handleError(error);
        }
    }


    async getJson(url: string): Promise<any> {
        try {
            const data = await this.httpService.get(this.getS1Server() + "/" + url,
                { headers: this.headers(), withCredentials: true }).toPromise();

            if (!data) {
                return void 0;
            }
            const json = data; // this.decode(url, data);
            return json;

        } catch (error) {
            throw this.handleError(error);
        }
    }

    async getRaw(url: string): Promise<any> {
        try {
            const data = await this.httpService.get(this.getS1Server() + "/" + url,
                { headers: this.headers(), withCredentials: true, responseType: "text" }).toPromise();

            if (!data) {
                return void 0;
            }
            
            return data;

        } catch (error) {
            throw this.handleError(error);
        }
    }

    private decode(url: string, data: ArrayBuffer) {
        const json = msgpack.decode(new Uint8Array(data));
        if (!environment.production) {
            console.log(url, "decoded json", json);
        }
        return json;
    }

    private encode(url: string, data: any): any {
        return data;
        if (!environment.production) {
            console.log(url, "encoded json", data);
        }
        return new Uint8Array(msgpack.encode(data)).buffer;
    }

    async post<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T> {
        try {
            const data = this.encode(url, body);
            const response = await this.httpService.post(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers() }).pipe(timeout(2000)).toPromise();

            if (!response) {
                return void 0;
            }

            const json = response; // this.decode(url, response);
            return BaseModel.getBaseModelFromJson<T>(json, void 0, this.translationService);

        } catch (error) {
            throw this.handleError(error);
        }
    }
    async postMultiple<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T[]> {
        try {
            const data = this.encode(url, body);
            const response = await this.httpService.post(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers() }).toPromise();

            if (!data) {
                return void 0;
            }

            const jsonArr = response; // this.decode(url, response);

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
            const data = this.encode(url, body);
            const response = await this.httpService.post(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers() }).toPromise();

            if (!data) {
                return void 0;
            }

            const json = response; // this.decode(url, response);
            return json;

        } catch (error) {
            throw this.handleError(error);
        }
    }

    async put<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T> {
        try {
            const data = this.encode(url, body);
            const response = await this.httpService.put(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers() }).toPromise();

            if (!response) {
                return void 0;
            }

            const json = response; // this.decode(url, response);
            return BaseModel.getBaseModelFromJson<T>(json, void 0, this.translationService);

        } catch (error) {
            throw this.handleError(error);
        }
    }

    async putJson(url: string, body: any, withCredentials: boolean = true): Promise<any> {
        try {
            const data = this.encode(url, body);
            const response = await this.httpService.put(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers() }).toPromise();

            if (!data) {
                return void 0;
            }

            const json = response; // this.decode(url, response);
            return json;

        } catch (error) {
            throw this.handleError(error);
        }
    }

    async patch<T extends BaseModel>(url: string, body: any, withCredentials: boolean = true): Promise<T> {
        try {
            const data = this.encode(url, body);
            const response = await this.httpService.patch(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers() }).toPromise();

            if (!response) {
                return void 0;
            }

            const json = response; // this.decode(url, response);
            return BaseModel.getBaseModelFromJson<T>(json, void 0, this.translationService);

        } catch (error) {
            throw this.handleError(error);
        }
    }

    async patchJson(url: string, body: any, withCredentials: boolean = true): Promise<any> {
        try {
            const data = this.encode(url, body);
            const response = await this.httpService.patch(this.getS1Server() + "/" + url, data,
                { withCredentials: withCredentials, headers: this.headers() }).toPromise();

            if (!response) {
                return void 0;
            }

            const json = response; // this.decode(url, response);
            return json;

        } catch (error) {
            throw this.handleError(error);
        }
    }

    async deleteJson(url: string, withCredentials: boolean = true): Promise<any> {
        try {
            const response = await this.httpService.delete(this.getS1Server() + "/" + url,
                { withCredentials: withCredentials, headers: this.headers() }).toPromise();

            const json = response; // this.decode(url, response);
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
        } else if (error.error && (error.error.hasOwnProperty("TypeInfo") || error.error.hasOwnProperty("typeInfo"))) {
            const ex = new WebApiException();
            BaseModel.fromJson(error.error, ex, this.translationService);
            return ex;
        }
        return error;
    }

}
