import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { DesignTimeDataService } from "./design-time-data.service";
import { Setting } from "../base/model/setting";

@Injectable()
export class SettingsService extends BaseService {


    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService, private designData: DesignTimeDataService) {
        super(http, pRouter, translationService);
    }

    getSettings(): Promise<Setting[]> {
        return this.getMultiple<Setting>("settings");
    }

    getByKey(key: string): Promise<Setting> {
        return this.get<Setting>("settings/key/" + key);
    }

    saveSettings(settings: Setting[]): Promise<Setting[]> {
        const data = new Array<any>();
        for (const set of settings) {
            data.push(set.toJson());
        }
        return super.postMultiple("settings", data);
    }
}
