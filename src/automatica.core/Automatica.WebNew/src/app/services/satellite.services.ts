import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { DesignTimeDataService } from "./design-time-data.service";
import { Satellite } from "../base/model/satellites/satellite";

@Injectable()
export class SatelliteService extends BaseService {

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService, private designData: DesignTimeDataService) {
        super(http, pRouter, translationService);
    }


    getAll(): Promise<Satellite[]> {
        return super.getMultiple<Satellite>("satellite");
    }

    save(satellites: Satellite[]) {
        const data = new Array<any>();
        for (const set of satellites) {
            data.push(set.toJson());
        }
        return super.postMultiple("satellite", data);
    }


}
