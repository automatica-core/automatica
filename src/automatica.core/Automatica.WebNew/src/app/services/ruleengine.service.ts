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
import { Link } from "../base/model/link";

export interface AddLogicData {
  data: RuleInstance | NodeInstance2RulePage;
  pageId: string;
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

  reload(): Promise<any> {
    return super.postJson("rules/reload", {});
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

  addPage(page: RulePage): Promise<RulePage> {
    return super.post<RulePage>("rules/page/add", page.toJson());
  }

  addItem(item: AddLogicData): Promise<any> {

    let linkExtension = "ruleInstance";
    if (item.data instanceof RuleInstance) {
      linkExtension = "ruleInstance";
    } else {
      linkExtension = "nodeInstance";
    }

    this.add.emit(item);

    return super.postJson(`rules/item/${linkExtension}/${item.pageId}`, item.data.toJson());
  }

  removePage(page: RulePage): Promise<any> {
    return super.deleteJson(`rules/page/${page.ObjId}`);
  }

  removeItem(data: RuleInstance | NodeInstance2RulePage): Promise<any> {

    let linkExtension = "ruleInstance";
    if (data instanceof RuleInstance) {
      linkExtension = "ruleInstance";
    } else {
      linkExtension = "nodeInstance";
    }

    return super.deleteJson(`rules/item/${linkExtension}/${data.ObjId}`);
  }

  updateItem(item: RuleInstance | NodeInstance2RulePage) {
    if (item instanceof RuleInstance) {
      return super.patchJson(`rules/item/ruleInstance`, item.toJson());
    } else {
      return super.patchJson(`rules/item/nodeInstance`, item.toJson());
    }
  }

  addOrUpdateLink(item: Link) {
    return super.postJson(`rules/link`, item.toJson());
  }

  removeLink(item: Link) {
    return super.deleteJson(`rules/link/${item.ObjId}`);
  }
}
