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
import { NodeInstance2RulePage } from "../base/model/node-instance-2-rule-page";
import { Link } from "../base/model/link";

export interface AddLogicData {
  data: RuleInstance | NodeInstance2RulePage;
  pageId: string;
}


@Injectable()
export class LogicEngineService extends BaseService {

  public reInit: EventEmitter<RulePage> = new EventEmitter<RulePage>();
  public add = new EventEmitter<AddLogicData>();

  constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService, private designService: DesignTimeDataService) {
    super(http, pRouter, translationService);
  }


  save(page: RulePage, model: any): Promise<RulePage> {
    return super.post<RulePage>("logics/save", page.toJson());
  }

  reload(): Promise<any> {
    return super.postJson("logics/reload", {});
  }

  getPages(): Promise<RulePage[]> {
    return super.getMultiple<RulePage>("logics/pages");
  }
  getPage(id: number): Promise<RulePage> {
    return super.get<RulePage>("logics/page/" + id);
  }

  getRuleTemplates(): Promise<RuleTemplate[]> {
    return this.designService.getRuleTemplates();
  }

  addPage(page: RulePage): Promise<RulePage> {
    return super.post<RulePage>("logics/page/add", page.toJson());
  }

  addItem(item: AddLogicData): Promise<any> {

    let linkExtension = "logicInstance";
    if (item.data instanceof RuleInstance) {
      linkExtension = "logicInstance";
    } else {
      linkExtension = "nodeInstance";
    }

    this.add.emit(item);

    return super.postJson(`logics/item/${linkExtension}/${item.pageId}`, item.data.toJson());
  }

  removePage(page: RulePage): Promise<any> {
    return super.deleteJson(`logics/page/${page.ObjId}`);
  }

  removeItem(data: RuleInstance | NodeInstance2RulePage): Promise<any> {

    let linkExtension = "logicInstance";
    if (data instanceof RuleInstance) {
      linkExtension = "logicInstance";
    } else {
      linkExtension = "nodeInstance";
    }

    return super.deleteJson(`logics/item/${linkExtension}/${data.ObjId}`);
  }

  updateItem(item: RuleInstance | NodeInstance2RulePage) {
    if (item instanceof RuleInstance) {
      return super.patchJson(`logics/item/logicInstance`, item.toJson());
    } else {
      return super.patchJson(`logics/item/nodeInstance`, item.toJson());
    }
  }

  addOrUpdateLink(item: Link) {
    return super.postJson(`logics/link`, item.toJson());
  }

  removeLink(item: Link) {
    return super.deleteJson(`logics/link/${item.ObjId}`);
  }

  updatePage(item: RulePage) {
    return super.patch("logics/page", item.toJson());
  }
}
