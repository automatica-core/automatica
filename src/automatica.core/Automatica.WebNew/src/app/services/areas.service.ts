import { Injectable, EventEmitter } from "@angular/core";
import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { DesignTimeDataService } from "./design-time-data.service";
import { AreaInstance, AreaTemplate } from "../base/model/areas";

@Injectable()
export class AreaService extends BaseService {


    public etsImported = new EventEmitter<AreaInstance[]>();

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService, private designData: DesignTimeDataService) {
        super(http, pRouter, translationService);
    }

    getAreaTemplates(): Promise<AreaTemplate[]> {
        return this.designData.getAreaTemplates();
    }

    getAreaInstances(): Promise<AreaInstance[]> {
        return super.getMultiple<AreaInstance>("areas");
    }

    saveAreaInstance(areaInstances: AreaInstance[]) {
        const data = new Array<any>();
        for (const set of areaInstances) {
            data.push(set.toJson());
        }
        return super.postMultiple("areas", data);
    }

    addAreaInstances(areaInstances: AreaInstance[]) {
        const data = new Array<any>();
        for (const set of areaInstances) {
            data.push(set.toJson());
        }
        return super.postMultiple<AreaInstance>("areas/add", data);
    }


}
