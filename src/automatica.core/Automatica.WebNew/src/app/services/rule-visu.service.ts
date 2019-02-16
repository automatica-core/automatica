import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";

@Injectable()
export class RuleInstanceVisuService extends BaseService {

    constructor(http: HttpClient, pRouter: Router, translationService: TranslationService) {
        super(http, pRouter, translationService);
    }

    public getRuleInstanceData(id: string): Promise<any> {
        return super.getJson("rules/data/" + id);
    }

}
