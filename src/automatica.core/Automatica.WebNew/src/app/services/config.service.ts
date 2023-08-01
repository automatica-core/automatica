import { Injectable, OnInit } from "@angular/core";
import { BaseService } from "./base-service";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { HttpClient } from "@angular/common/http";
import { DesignTimeDataService } from "./design-time-data.service";
import { NodeTemplate } from "../base/model/node-template";
import { NodeInstance } from "../base/model/node-instance";
import { PropertyInstance } from "../base/model/property-instance";

@Injectable()
export class ConfigService extends BaseService {


    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService, private designData: DesignTimeDataService) {
        super(http, pRouter, translationService);
    }

    async loadConfiguration() {

    }

    getSingleNodeInstance(id: string): Promise<NodeInstance> {
        return super.get("nodeInstances/single/" + id);
    }

    getNodeInstances(): Promise<NodeInstance[]> {
        return super.getMultiple<NodeInstance>("nodeInstances");
    }

    getNodeInstancesLinkedToArea(area: string) {
        return super.getMultiple<NodeInstance>("nodeInstances/areaLinked/" + area);
    }


    reInitServer(): Promise<void> {
        return super.postJson("server", "");
    }

    getServerState(): Promise<any> {
        return super.getJson("server/state");
    }

    scan(node: NodeInstance): Promise<NodeInstance[]> {
        return super.postMultiple<NodeInstance>("nodeInstancesV2/scan", node.toJson());
    }

    import(node: NodeInstance, fileName: string, password: string): Promise<NodeInstance[]> {
        return super.postMultiple<NodeInstance>("nodeInstancesV2/import/", { Node: node.toJson(), FileName: fileName, Password: password });
    }
    read(node: NodeInstance): Promise<any> {
        return super.postJson("nodeInstances/read", node.toJson());
    }
    customAction(node: NodeInstance, actionName: string): Promise<NodeInstance[]> {
        return super.postMultiple<NodeInstance>("nodeInstances/customAction/" + actionName, node.toJson());
    }

    save(nodeInstances: NodeInstance[]): Promise<NodeInstance[]> {
        const data = new Array<any>();
        for (const set of nodeInstances) {
            data.push(set.toJson());
        }
        return super.postMultiple<NodeInstance>("nodeInstances", data);
    }

    async add(nodeInstance: NodeInstance): Promise<NodeInstance> {
        return await super.put<NodeInstance>("nodeInstancesV2/add", nodeInstance.toJson());
    }

    async update(nodeInstance: NodeInstance): Promise<NodeInstance> {
        return await super.post<NodeInstance>("nodeInstancesV2/update", nodeInstance.toJson());
    }

    async reload(): Promise<any> {
        return await super.postJson("nodeInstancesV2/reload", void 0);
    }

    async delete(nodeInstance: NodeInstance): Promise<void> {
        return await super.postJson("nodeInstancesV2/delete", nodeInstance.toJson());
    }

    getLinkableNodes(): Promise<NodeInstance[]> {
        return super.getMultiple<NodeInstance>("nodeInstances/linkable");
    }

    async getVersion() {
        return await super.getJson("version");
    }

    enableLearnMode(nodeInstance: NodeInstance) {
        return super.postJson("nodeInstances/enableLeranMode", nodeInstance.toJson());
    }

    createFromTemplate(parentNode: NodeInstance, nodeTemplate: NodeTemplate) {
        return super.post<NodeInstance>(`nodeInstancesV2/create/${this.translationService.getLocale().language}/${parentNode.ObjId}/${nodeTemplate.ObjId}`, void 0);
    }

    copy(item: NodeInstance, target: NodeInstance) {
        return super.post<NodeInstance>(`nodeInstancesV2/copy/${item.ObjId}/${target.ObjId}`, void 0);
    }
}
