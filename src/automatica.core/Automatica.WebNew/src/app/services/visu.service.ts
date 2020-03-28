import { BaseService } from "../services/base-service";
import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { VisuObjectTemplate } from "../base/model/visu-object-template";
import { VisuPage } from "../base/model/visu-page";
import { BaseModel } from "../base/model/base-model";

@Injectable()
export class VisuService extends BaseService {
    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    getVisuTemplates(): Promise<VisuObjectTemplate[]> {
        return super.getMultiple<VisuObjectTemplate>("visualization/templates");
    }

    getVisuPages(): Promise<VisuPage[]> {
        return super.getMultiple<VisuPage>("visualization/pages");
    }
    getVisuPage(id: string): Promise<BaseModel> {
        return super.get("visualization/page/" + id);
    }

    getDefaultVisuPage(pageType: number): Promise<VisuPage> {
        return super.get<VisuPage>("visualization/page/default/" + pageType);
    }

    getFavorites(): Promise<BaseModel> {
        return super.get("visualization/page/default/0");
    }

    saveVisuPages(pages: VisuPage[]): Promise<VisuPage[]> {
        const ar = [];
        for (const page of pages) {
            ar.push(page.toJson());
        }

        return super.postMultiple<VisuPage>("visualization/pages", ar);
    }
}
