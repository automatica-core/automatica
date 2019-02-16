import { BaseService } from "./base-service";
import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";

@Injectable()
export class DataService extends BaseService {

    constructor(httpService: HttpClient, pRouter: Router, translationService: TranslationService) {
        super(httpService, pRouter, translationService);
    }
    public getCurrentNodeValues(): Promise<any> {
        return super.getJson("data/node/current");
    }

}
