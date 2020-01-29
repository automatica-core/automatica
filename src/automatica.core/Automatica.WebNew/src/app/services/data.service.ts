import { BaseService } from "./base-service";
import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { Trending } from "../base/model/trending/trending";

@Injectable()
export class DataService extends BaseService {

    constructor(httpService: HttpClient, pRouter: Router, translationService: TranslationService) {
        super(httpService, pRouter, translationService);
    }
    public getCurrentNodeValues(): Promise<any> {
        return super.getJson("data/node/current");
    }

    public getTrendings(nodeId: string, startDate: Date, endDate: Date) {
        return super.getMultiple<Trending>(`data/trend/${nodeId}/${startDate.toISOString()}/${endDate.toISOString()}/${startDate.toISOString()}/${endDate.toISOString()}`);
    }

}
