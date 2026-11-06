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
        const values = await super.getJson("data/all/current") ?? {};

        for (const key of Object.keys(values)) {
            this._latestValues.set(key, values[key]);
        }

        return values;
    }

    public getLocalValue(id: string): NodeInstanceValue {
        if (this._latestValues.has(id)) {
            return this._latestValues.get(id);
        }
        return void 0;
    }

    /**
     * Keeps the snapshot current with the pushed values, controls created later start with the latest value.
     */
    public setLocalValue(id: string, value: NodeInstanceValue) {
        this._latestValues.set(id, value);
    }

    public getTrendings(nodeId: string, startDate: Date, endDate: Date) {
        return super.getMultiple<Trending>(`data/trend/${nodeId}/${startDate.toISOString()}/${endDate.toISOString()}/${startDate.toISOString()}/${endDate.toISOString()}`);
    }

}
