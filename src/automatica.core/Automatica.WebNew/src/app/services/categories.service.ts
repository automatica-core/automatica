import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { DesignTimeDataService } from "./design-time-data.service";
import { CategoryGroup, CategoryInstance } from "../base/model/categories";

@Injectable()
export class CategoryService extends BaseService {
    

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService, private designData: DesignTimeDataService) {
        super(http, pRouter, translationService);
    }

    getCategoryGroups(): Promise<CategoryGroup[]> {
        return this.designData.getCategoryGroups();
    }

    getCategoryInstances(): Promise<CategoryInstance[]> {
        return super.getMultiple<CategoryInstance>("categories");
    }

    getAllCategoryInstances(): Promise<CategoryInstance[]> {
        return super.getMultiple<CategoryInstance>("categories/all");
    }

    removeCategoryInstance(categoryInstance: CategoryInstance){
        return super.deleteJson("categories/" + categoryInstance.ObjId);
    }

    saveCategoryInstance(categoryInstance: CategoryInstance) {
        let json = categoryInstance.toJson();
        return super.putJson("categories", json);
    }

    saveCategoryInstances(categoryInstances: CategoryInstance[]) {
        const data = new Array<any>();
        for (const set of categoryInstances) {
            data.push(set.toJson());
        }
        return super.postMultiple("categories", data);
    }


}
