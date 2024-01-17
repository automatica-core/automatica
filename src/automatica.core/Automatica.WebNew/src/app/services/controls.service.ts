import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { Control } from "../base/model/control";

@Injectable()
export class ControlsService extends BaseService {

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    getAll(): Promise<Control[]> {
        return super.getMultiple<Control>("controls/all");
    }
}
