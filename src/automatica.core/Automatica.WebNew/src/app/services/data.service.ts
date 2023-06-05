import { BaseService } from "./base-service";
import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { Trending } from "../base/model/trending/trending";

export interface NodeInstanceValue {
    type: number;
    id: string;
    value: any;
    timestamp: Date;
}

@Injectable()
export class DataService extends BaseService {

    private _latestValues: Map<string, any> = new Map<string, any>();

    constructor(httpService: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(httpService, pRouter, translationService);
    }
    public getCurrentNodeValues(): Promise<any> {
        return super.getJson("data/node/current");
    }

    public async getAllValues(): Promise<any> {
        const values = await super.getJson("data/all/current");
        const that = this;

        Object.keys(values).forEach(function (key) {
            that._latestValues.set(key, values[key]);
        });

        return values;
    }

    public getLocalValue(id: string): NodeInstanceValue {
        if (this._latestValues.has(id)) {
            return this._latestValues.get(id);
        }
        return void 0;
    }

    public getTrendings(nodeId: string, startDate: Date, endDate: Date) {
        return super.getMultiple<Trending>(`data/trend/${nodeId}/${startDate.toISOString()}/${endDate.toISOString()}/${startDate.toISOString()}/${endDate.toISOString()}`);
    }

}
