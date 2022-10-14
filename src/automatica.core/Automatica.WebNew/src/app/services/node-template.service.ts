import { BaseService } from "./base-service";
import { Injectable } from "@angular/core";
import { NodeInstance } from "../base/model/node-instance";
import { NodeTemplate } from "../base/model/node-template";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

@Injectable()
export class NodeTemplateService extends BaseService {

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }


    getSupportedTemplates(treeNode: NodeInstance, neededInterfaceKey: string) {
        return super.getMultiple<NodeTemplate>(`nodeTemplates/supported/${treeNode.ObjId}/${neededInterfaceKey}`);
    }
}
