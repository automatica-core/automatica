import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { BaseService } from "./base-service";
import { SecurityControlInfo, SecurityState } from "../base/model/security/security";

/**
 * webapi/security-visu: state and commands of the security logics. Reading needs a signed-in role that may see the
 * logic, the commands need the roles visu or administrator. Command arguments may hold codes: never log them.
 */
@Injectable()
export class SecurityVisuService extends BaseService {

    private static readonly base = "security-visu";

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    getState(controlId: string): Promise<SecurityControlInfo> {
        return super.getJson(`${SecurityVisuService.base}/${controlId}`);
    }

    async command(controlId: string, command: string, args?: { [key: string]: string }): Promise<SecurityState | undefined> {
        const result = await super.postJson(`${SecurityVisuService.base}/${controlId}/command`, { command, arguments: args ?? {} });
        return result?.state;
    }
}
