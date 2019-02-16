import { Injectable } from "@angular/core";
import { BaseService } from "../services/base-service";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { HttpClient } from "@angular/common/http";

@Injectable()
export class SystemService extends BaseService {

    constructor(httpService: HttpClient, pRouter: Router, translationService: TranslationService) {
        super(httpService, pRouter, translationService);
    }

    checkForUpdate(): Promise<any> {
        return super.getJson("update/checkForUpdate");
    }
    alreadyDownloaded(): Promise<any> {
        return super.getJson("update/alreadyDownloaded");
    }

    downloadUpdate(update: any): Promise<any> {
        return super.postJson("update/download", update);
    }
    installUpdate(update: any): Promise<any> {
        return super.postJson("update/install", update);
    }

}
