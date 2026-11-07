import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { BaseService } from "./base-service";
import { AutomationDefinition } from "../base/model/automations/automation-definition";
import {
    AutomationDto, AutomationRunDto, AutomationSaveDto, RunStartedDto, TestResult, ValidationProblem
} from "../base/model/automations/automation-dto";

/**
 * webapi/automations (admin only). The DTOs are plain camelCase JSON, not BaseModels.
 */
@Injectable()
export class AutomationsService extends BaseService {

    private static readonly base = "automations";

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    async getAll(): Promise<AutomationDto[]> {
        const list = await super.getJson(AutomationsService.base);
        return (list || []).map(AutomationsService.normalize);
    }

    async getById(id: string): Promise<AutomationDto> {
        return AutomationsService.normalize(await super.getJson(`${AutomationsService.base}/${id}`));
    }

    async create(dto: AutomationSaveDto): Promise<AutomationDto> {
        return AutomationsService.normalize(await super.postJson(AutomationsService.base, dto));
    }

    async update(id: string, dto: AutomationSaveDto): Promise<AutomationDto> {
        return AutomationsService.normalize(await super.putJson(`${AutomationsService.base}/${id}`, dto));
    }

    delete(id: string): Promise<any> {
        return super.deleteJson(`${AutomationsService.base}/${id}`);
    }

    async setEnabled(id: string, enabled: boolean): Promise<AutomationDto> {
        return AutomationsService.normalize(await super.postJson(`${AutomationsService.base}/${id}/enabled`, { enabled }));
    }

    run(id: string, skipConditions: boolean = false): Promise<RunStartedDto> {
        return super.postJson(`${AutomationsService.base}/${id}/run?skipConditions=${skipConditions}`, {});
    }

    stopRun(id: string, runId: string): Promise<any> {
        return super.postJson(`${AutomationsService.base}/${id}/runs/${runId}/stop`, {});
    }

    async getRuns(id: string): Promise<AutomationRunDto[]> {
        return (await super.getJson(`${AutomationsService.base}/${id}/runs`)) || [];
    }

    async validate(definition: AutomationDefinition): Promise<ValidationProblem[]> {
        return (await super.postJson(`${AutomationsService.base}/validate`, { definition })) || [];
    }

    test(definition: AutomationDefinition, triggerId: string | null): Promise<TestResult> {
        return super.postJson(`${AutomationsService.base}/test`, { definition, triggerId });
    }

    async whereUsed(objectId: string): Promise<AutomationDto[]> {
        return ((await super.getJson(`${AutomationsService.base}/where-used/${objectId}`)) || []).map(AutomationsService.normalize);
    }

    /** the ValidationProblem[] of a 400 answer (saving an enabled rule with errors), otherwise undefined */
    static problemsOfError(error: any): ValidationProblem[] | undefined {
        if (error?.status === 400 && Array.isArray(error.error)) {
            return error.error;
        }
        return void 0;
    }

    /** fills the parts of the definition an older or partial document may lack */
    static normalize(dto: AutomationDto): AutomationDto {
        if (!dto) {
            return dto;
        }
        let def: any = dto.definition;
        if (typeof def === "string") {
            try {
                def = JSON.parse(def);
            } catch {
                def = void 0;
            }
        }
        def = def || {};
        dto.definition = {
            schema: def.schema ?? 1,
            triggers: def.triggers ?? [],
            conditions: def.conditions ?? { type: "group", operator: "all", conditions: [] },
            actions: def.actions ?? []
        };
        dto.problems = dto.problems ?? [];
        return dto;
    }
}
