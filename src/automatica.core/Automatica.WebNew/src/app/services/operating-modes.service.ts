import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { BaseService } from "./base-service";
import { Holiday, OperatingModeConfiguration, OperatingModeOverview, OperatingModeVisuState } from "../base/model/operating-modes/operating-modes";

/**
 * webapi/operating-modes (configuration, admins) and webapi/operating-modes-visu (state for every role, manual switch for
 * visualization users and admins). Plain camelCase JSON.
 */
@Injectable()
export class OperatingModesService extends BaseService {

    private static readonly base = "operating-modes";
    private static readonly visu = "operating-modes-visu";

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    getOverview(days = 31): Promise<OperatingModeOverview> {
        return super.getJson(`${OperatingModesService.base}?days=${days}`);
    }

    save(configuration: OperatingModeConfiguration): Promise<OperatingModeOverview> {
        return super.putJson(OperatingModesService.base, configuration);
    }

    async holidays(country: string, region: string, year: number): Promise<Holiday[]> {
        const query = `country=${encodeURIComponent(country || "")}&region=${encodeURIComponent(region || "")}&year=${year}`;
        return (await super.getJson(`${OperatingModesService.base}/holidays?${query}`)) || [];
    }

    getState(): Promise<OperatingModeVisuState> {
        return super.getJson(OperatingModesService.visu);
    }

    /** on (true), off (false) or back to the operating times (null) */
    setManual(mode: string, state: boolean | null): Promise<any> {
        return super.postJson(`${OperatingModesService.visu}/${encodeURIComponent(mode)}/manual`, { state });
    }
}
