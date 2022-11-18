import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { HttpClient } from "@angular/common/http";
import { NodeTemplate } from "../base/model/node-template";
import { AreaTemplate } from "../base/model/areas";
import { RuleTemplate } from "../base/model/rule-template";
import { CategoryGroup } from "../base/model/categories";

@Injectable()
export class DesignTimeDataService extends BaseService {

    private _areaTemplates: AreaTemplate[];
    private _ruleTemplates: RuleTemplate[];
    private _categoryGroups: CategoryGroup[];

    private _nodeTemplateCache: Map<string, NodeTemplate> = new Map<string, NodeTemplate>();

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    public async getNodeTemplate(id: string): Promise<NodeTemplate> {

        if(this._nodeTemplateCache.has(id)) {
            return this._nodeTemplateCache.get(id);
        }
        var template = await super.get<NodeTemplate>(`nodeTemplates/${id}`);

        this._nodeTemplateCache.set(id, template);

        return template;
    }

    async getAreaTemplates(): Promise<AreaTemplate[]> {
        if (!this._areaTemplates) {
            this._areaTemplates = await super.getMultiple<AreaTemplate>("areas/templates");
        }

        return Promise.resolve(this._areaTemplates);
    }

    async getCategoryGroups(): Promise<CategoryGroup[]> {
        if (!this._categoryGroups) {
            this._categoryGroups = await super.getMultiple<CategoryGroup>("categories/groups");
        }

        return Promise.resolve(this._categoryGroups);
    }

    async getRuleTemplates(): Promise<RuleTemplate[]> {
        if (!this._ruleTemplates) {
            this._ruleTemplates = await super.getMultiple<RuleTemplate>("rules/templates");
        }

        return Promise.resolve(this._ruleTemplates);
    }

}
