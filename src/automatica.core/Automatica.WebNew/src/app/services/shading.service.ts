import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { BaseService } from "./base-service";
import {
    AvailableBlind, BlindDefinition, ShadingBlind, ShadingFacade, ShadingPreview, ShadingProblem, ShadingSettings, ShadingStatus,
    SunPathPoint, VisuBlindState
} from "../base/model/shading/shading";

/**
 * webapi/shading (configuration and status, admins only) and webapi/shading-visu (the visualization's auto switch).
 * The DTOs are plain camelCase JSON, not BaseModels.
 */
@Injectable()
export class ShadingService extends BaseService {

    private static readonly base = "shading";
    private static readonly visu = "shading-visu";

    /** one request for all blind tiles of a visualization page */
    private visuStates: Promise<Map<string, VisuBlindState>> | undefined;
    private visuStatesAt = 0;

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    // ---------- configuration ----------

    async getSettings(): Promise<ShadingSettings> {
        return (await super.getJson(`${ShadingService.base}/settings`))?.definition;
    }

    async saveSettings(definition: ShadingSettings): Promise<ShadingSettings> {
        return (await super.putJson(`${ShadingService.base}/settings`, { definition }))?.definition;
    }

    async getFacades(): Promise<ShadingFacade[]> {
        return (await super.getJson(`${ShadingService.base}/facades`)) || [];
    }

    createFacade(facade: ShadingFacade): Promise<ShadingFacade> {
        return super.postJson(`${ShadingService.base}/facades`, facade);
    }

    updateFacade(facade: ShadingFacade): Promise<ShadingFacade> {
        return super.putJson(`${ShadingService.base}/facades/${facade.objId}`, facade);
    }

    deleteFacade(id: string): Promise<any> {
        return super.deleteJson(`${ShadingService.base}/facades/${id}`);
    }

    async getBlinds(): Promise<ShadingBlind[]> {
        return (await super.getJson(`${ShadingService.base}/blinds`)) || [];
    }

    async getAvailableBlinds(): Promise<AvailableBlind[]> {
        return (await super.getJson(`${ShadingService.base}/blinds/available`)) || [];
    }

    saveBlind(controlId: string, dto: { this2ShadingFacade?: string | null; isEnabled: boolean; definition: BlindDefinition }): Promise<ShadingBlind> {
        return super.putJson(`${ShadingService.base}/blinds/${controlId}`, dto);
    }

    deleteBlind(controlId: string): Promise<any> {
        return super.deleteJson(`${ShadingService.base}/blinds/${controlId}`);
    }

    setAutomatic(controlId: string, enabled: boolean): Promise<any> {
        return super.postJson(`${ShadingService.base}/blinds/${controlId}/automatic`, { enabled });
    }

    resume(controlId: string): Promise<any> {
        return super.postJson(`${ShadingService.base}/blinds/${controlId}/resume`, {});
    }

    // ---------- status ----------

    getStatus(): Promise<ShadingStatus> {
        return super.getJson(`${ShadingService.base}/status`);
    }

    getPreview(at: Date): Promise<ShadingPreview> {
        return super.getJson(`${ShadingService.base}/preview?at=${encodeURIComponent(at.toISOString())}`);
    }

    async getSunPath(date: Date, stepMinutes = 15): Promise<SunPathPoint[]> {
        const day = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
        return (await super.getJson(`${ShadingService.base}/sun-path?date=${day}&stepMinutes=${stepMinutes}`)) || [];
    }

    // ---------- visualization ----------

    /** the state of the configured blinds, shared by the tiles for a few seconds */
    visuBlinds(maxAgeMs = 10000): Promise<Map<string, VisuBlindState>> {
        if (!this.visuStates || Date.now() - this.visuStatesAt > maxAgeMs) {
            this.visuStatesAt = Date.now();
            this.visuStates = super.getJson(`${ShadingService.visu}/blinds`)
                .then((list: VisuBlindState[]) => new Map((list || []).map(a => [a.controlId.toLowerCase(), a])))
                .catch(e => {
                    // the visualization works without the shading state
                    this.visuStates = void 0;
                    console.warn("shading: no blind states", e);
                    return new Map<string, VisuBlindState>();
                });
        }
        return this.visuStates;
    }

    async setVisuAutomatic(controlId: string, enabled: boolean): Promise<void> {
        await super.postJson(`${ShadingService.visu}/blinds/${controlId}/automatic`, { enabled });
        this.visuStates = void 0;
    }

    async resumeVisu(controlId: string): Promise<void> {
        await super.postJson(`${ShadingService.visu}/blinds/${controlId}/resume`, {});
        this.visuStates = void 0;
    }

    /** the problems of a 400 answer, otherwise undefined */
    static problemsOfError(error: any): ShadingProblem[] | undefined {
        if (error?.status === 400 && Array.isArray(error.error)) {
            return error.error;
        }
        return void 0;
    }
}
