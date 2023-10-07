import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { HttpClient } from "@angular/common/http";
import { AggregatedValueRecord, AggregationType } from "../base/model/aggregated-record-value";

@Injectable()
export class HyperSeriesService extends BaseService {
    constructor(httpService: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(httpService, pRouter, translationService);
    }

    async getAggregatedValues(aggregationType: AggregationType, nodeInstanceId: string, start?: Date, end?: Date, count?: number): Promise<AggregatedValueRecord[]> {
        var json = await super.getJson(`hyperseries/${aggregationType}?id=${nodeInstanceId}&startDate=${start?.toISOString()}&endDate=${end?.toISOString()}&count=${count}`);

        return json;
    }
}