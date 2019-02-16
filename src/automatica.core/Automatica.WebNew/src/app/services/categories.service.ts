import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { DesignTimeDataService } from "./design-time-data.service";
import { CategoryGroup, CategoryInstance } from "../base/model/categories";

@Injectable()
export class CategoryService extends BaseService {

    constructor(http: HttpClient, pRouter: Router, translationService: TranslationService, private designData: DesignTimeDataService) {
        super(http, pRouter, translationService);
    }

    getCategoryGroups(): Promise<CategoryGroup[]> {
        return this.designData.getCategoryGroups();
    }

    getCategoryInstances(): Promise<CategoryInstance[]> {
        return super.getMultiple<CategoryInstance>("categories");
    }

    saveAreaInstance(areaInstances: CategoryInstance[]) {
        const data = new Array<any>();
        for (const set of areaInstances) {
            data.push(set.toJson());
        }
        return super.postMultiple("categories", data);
    }


}
