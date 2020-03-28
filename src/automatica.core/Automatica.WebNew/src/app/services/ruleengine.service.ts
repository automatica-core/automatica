import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { BaseService } from "./base-service";
import { EventEmitter } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { DesignTimeDataService } from "./design-time-data.service";
import { RulePage } from "../base/model/rule-page";
import { RuleTemplate } from "../base/model/rule-template";
import { RuleInstance } from "../base/model/rule-instance";
import { NodeInstance } from "../base/model/node-instance";
import { NodeInstance2RulePage } from "../base/model/node-instance-2-rule-page";
import { Model, JsonProperty, JsonFieldInfo, BaseModel } from "../base/model/base-model";

export interface AddLogicData {
  data: RuleInstance | NodeInstance2RulePage;
  pageIndex: string;
}

@Model()
export class SaveAllLogicEditor extends BaseModel {
  @JsonProperty()
  LogicPages: RulePage[] = [];

  @JsonProperty()
  NodeInstances: NodeInstance[] = [];

  public typeInfo(): string {
    return "SaveAllLogicEditor";
  }

  protected getJsonProperty(): Map<string, JsonFieldInfo> {
    return void 0;
  }
}

@Injectable()
export class RuleEngineService extends BaseService {

  public reInit: EventEmitter<number> = new EventEmitter<number>();
  public add = new EventEmitter<AddLogicData>();

  constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService, private designService: DesignTimeDataService) {
    super(http, pRouter, translationService);
  }


  save(page: RulePage, model: any): Promise<RulePage> {
    return super.post<RulePage>("rules/save", page.toJson());
  }

  saveAll(pages: RulePage[], nodeInstances: NodeInstance[]): Promise<SaveAllLogicEditor> {
    const pagesJson = [];
    for (const page of pages) {
      pagesJson.push(page.toJson());
    }

    const nodesJson = new Array<any>();
    for (const set of nodeInstances) {
      nodesJson.push(set.toJson());
    }
    return super.post<SaveAllLogicEditor>("rules/saveAll", { logicPages: pagesJson, nodeInstances: nodesJson });
  }

  getPages(): Promise<RulePage[]> {
    return super.getMultiple<RulePage>("rules/pages");
  }
  getPage(id: number): Promise<RulePage> {
    return super.get<RulePage>("rules/page/" + id);
  }

  getRuleTemplates(): Promise<RuleTemplate[]> {
    return this.designService.getRuleTemplates();
  }
}
