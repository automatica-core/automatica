import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { Injectable } from "@angular/core";

@Injectable()
export class PingService extends BaseService {
    constructor(http: HttpClient, pRouter: Router, translationService: TranslationService) {
        super(http, pRouter, translationService);
    }

    public ping(url: string): Promise<any> {
        return super.getAbsoluteJson("http://" + url + "/webapi/ping");
    }
}
