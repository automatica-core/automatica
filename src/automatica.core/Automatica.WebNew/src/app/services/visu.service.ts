import { BaseService } from "../services/base-service";
import { EventEmitter, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { VisuObjectTemplate } from "../base/model/visu-object-template";
import { VisuPage } from "../base/model/visu-page";
import { BaseModel } from "../base/model/base-model";
import { VisualizationDataFacade } from "../base/model/visualization-data-facade";

@Injectable()
export class VisuService extends BaseService {

    public reloadedPage = new EventEmitter<VisuPage | VisualizationDataFacade>();

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    getVisuTemplates(): Promise<VisuObjectTemplate[]> {
        return super.getMultiple<VisuObjectTemplate>("visualization/templates");
    }

    getVisuPages(): Promise<VisuPage[]> {
        return super.getMultiple<VisuPage>("visualization/pages");
    }
    getVisuPage(id: string): Promise<VisuPage | VisualizationDataFacade> {
        return super.get("visualization/page/" + id);
    }

    getDefaultVisuPage(pageType: number): Promise<VisuPage> {
        return super.get<VisuPage>("visualization/page/default/" + pageType);
    }

    getFavorites(): Promise<VisualizationDataFacade> {
        return super.get("visualization/page/default/0");
    }

    saveVisuPages(pages: VisuPage[]): Promise<VisuPage[]> {
        const ar = [];
        for (const page of pages) {
            ar.push(page.toJson());
        }

        return super.postMultiple<VisuPage>("visualization/pages", ar);
    }

    async reloadPage(page: VisuPage): Promise<VisuPage | VisualizationDataFacade> {

        var loaded = await super.get<VisuPage>("visualization/page/" + page.ObjId);
        loaded.ObjId = page.ObjId;
        this.reloadedPage.emit(loaded);
        return loaded;

    }
}
