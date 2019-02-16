import { Injectable, OnInit } from "@angular/core";
import { BaseService } from "./base-service";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { HttpClient } from "@angular/common/http";
import { DesignTimeDataService } from "./design-time-data.service";
import { NodeTemplate } from "../base/model/node-template";
import { NodeInstance } from "../base/model/node-instance";
import { BoardType } from "../base/model/board-type";

@Injectable()
export class ConfigService extends BaseService {

    constructor(http: HttpClient, pRouter: Router, translationService: TranslationService, private designData: DesignTimeDataService) {
        super(http, pRouter, translationService);
    }

    getSingleNodeInstance(id: string): Promise<NodeInstance> {
        return super.get("nodeInstances/single/" + id);
    }


    getNodeTemplates(): Promise<NodeTemplate[]> {
        return this.designData.getNodeTemplates();
    }

    getNodeInstances(): Promise<NodeInstance[]> {
        return super.getMultiple<NodeInstance>("nodeInstances");
    }

    getBoardType(): Promise<BoardType> {
        return super.get<BoardType>("boardType");
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
        return super.postMultiple<NodeInstance>("nodeInstances/scan", node.toJson());
    }

    import(node: NodeInstance, fileName: string): Promise<NodeInstance[]> {
        return super.postMultiple<NodeInstance>("nodeInstances/import/", { Node: node.toJson(), FileName: fileName });
    }
    read(node: NodeInstance): Promise<any> {
        return super.postJson("nodeInstances/read", node.toJson());
    }

    save(nodeInstances: NodeInstance[]): Promise<NodeInstance[]> {
        const data = new Array<any>();
        for (const set of nodeInstances) {
            data.push(set.toJson());
        }
        return super.postMultiple<NodeInstance>("nodeInstances", data);
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
}
