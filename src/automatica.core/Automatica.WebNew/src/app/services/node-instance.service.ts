import { Injectable } from "@angular/core";
import { DesignTimeDataService } from "./design-time-data.service";
import { ConfigService } from "./config.service";
import { NodeInstance } from "../base/model/node-instance";
import { NodeTemplate } from "../base/model/node-template";
import { PropertyInstance } from "../base/model/property-instance";
import { VirtualSettingsPropertyInstance } from "../base/model/virtual-props/settings/settings-property-instance";
import { SettingsService } from "./settings.service";
import { Setting } from "../base/model/setting";
import { faTintSlash } from "@fortawesome/free-solid-svg-icons";
import { ITreeNode } from "../base/model/ITreeNode";
import { DataHubService } from "../base/communication/hubs/data-hub.service";

@Injectable()
export class NodeInstanceService {

    private _nodeTemplates: NodeTemplate[];
    private _nodeInstanceMap: Map<string, NodeInstance>;
    private _nodeInstanceList: NodeInstance[];
    private _settings: Setting[];

    public get nodeInstanceList(): NodeInstance[] {
        return this._nodeInstanceList;
    }

    constructor(private designTimeDataService: DesignTimeDataService,
        private configService: ConfigService,
        private settingsService: SettingsService,
        private dataHubService: DataHubService) {

    }

    public async load() {
        this._nodeTemplates = await this.configService.getNodeTemplates();
        this._settings = await this.settingsService.getSettings();
        await this.loadConfig();
        await this.dataHubService.subscribeForAll();

    }

    public getNodeInstance(id: string) {
        return this._nodeInstanceMap.get(id);
    }

    public hasNodeInstance(id: string) {
        if (!this._nodeInstanceMap) {
            return false;
        }
        return this._nodeInstanceMap.has(id);
    }

    public setNodeInstanceValue(id: string, value: any) {
        if (this.hasNodeInstance(id)) {
            this._nodeInstanceMap.get(id).Value = value;
        }
    }

    private async loadConfig() {
        const instances = await this.configService.getNodeInstances();

        this.convertConfig(instances);
    }

    public convertConfig(instances: NodeInstance[]) {
        this._nodeInstanceMap = new Map<string, NodeInstance>();

        this._nodeInstanceList = [];

        const tmpConfig: NodeInstance[] = [];

        for (const node of instances) {
            this.addNodeInstancesRec(node, tmpConfig);
        }
        tmpConfig.sort((a, b) => (a.Name.localeCompare(b.Name) - (b.Name.localeCompare(a.Name))));

        this._nodeInstanceList = tmpConfig;

        const rootNode = this._nodeInstanceList.filter(a => !a.This2ParentNodeInstance)[0];
        const rootNodeSettings: VirtualSettingsPropertyInstance[] = [];

        for (const x of this._settings) {
            rootNodeSettings.push(new VirtualSettingsPropertyInstance(x));
        }

        rootNode.Properties = [...rootNode.Properties, ...rootNodeSettings];

        return this._nodeInstanceList;
    }

    public addNodeInstance(node: NodeInstance) {
        this._nodeInstanceMap.set(node.Id, node);
        this._nodeInstanceList = [...this._nodeInstanceList, node];
    }

    public removeItem(node: ITreeNode) {
        this._nodeInstanceList = this._nodeInstanceList.filter(a => a.Id !== node.Id);
        this._nodeInstanceMap.delete(node.Id);
    }

    public addNodeInstancesRec(node: NodeInstance, tmpConfig: NodeInstance[]) {

        this._nodeInstanceMap.set(node.Id, node);
        tmpConfig.push(node);

        for (const x of node.Children) {
            this.addNodeInstancesRec(x, tmpConfig);
        }
    }

    public async createFromNodeTemplate(baseNode: NodeInstance, nodeTemplate: NodeTemplate, propertyInstances: PropertyInstance[]): Promise<{ node: NodeInstance, created: NodeInstance[] }> {
        const cachedNodeTemplate = this.designTimeDataService.getNodeTemplate(nodeTemplate.ObjId);
        const node = NodeInstance.createForNodeInstanceFromTemplate(cachedNodeTemplate, void 0);
        let created = [node];

        if (node) {
            for (const prop of propertyInstances) {
                const nodeProp = node.Properties.find(a => a.PropertyTemplate.Key === prop.PropertyTemplate.Key);

                console.log(nodeProp);

                if (nodeProp) {
                    nodeProp.Value = prop.Value;
                }
            }
        }

        let parent = void 0;
        if (baseNode.NodeTemplate.ProvidesInterface2InterfaceType === nodeTemplate.NeedsInterface2InterfacesType) {
            parent = baseNode;
        }

        parent = this.getNodeInstanceByNeedsInterface(baseNode, nodeTemplate.NeedsInterface2InterfacesType);

        if (!parent) {
            const allNodeTemplates = await this.designTimeDataService.getNodeTemplates();
            const nextTemplate = allNodeTemplates.find(a => a.ProvidesInterface2InterfaceType === nodeTemplate.NeedsInterface2InterfacesType);

            const newNode = await this.createFromNodeTemplate(baseNode, nextTemplate, propertyInstances);

            if (newNode) {
                parent = newNode.node;
                created = [...created, ...newNode.created];
            }
        }

        if (parent) {
            node.Parent = parent;
            node.This2ParentNodeInstance = parent.ObjId;
        }
        return { node: node, created: created };
    }

    public getNodeInstanceByNeedsInterface(nodeInstance: NodeInstance, needsInterfaceGuid: string): NodeInstance {
        if (nodeInstance.NodeTemplate.ProvidesInterface2InterfaceType === needsInterfaceGuid) {
            return nodeInstance;
        }

        if (!nodeInstance.Children || nodeInstance.Children.length === 0) {
            return void 0;
        }
        for (const x of nodeInstance.Children) {
            if (x.NodeTemplate.ProvidesInterface2InterfaceType === needsInterfaceGuid) {
                return x;
            }

            const child = this.getNodeInstanceByNeedsInterface(x, needsInterfaceGuid);
            if (child) {
                return child;
            }
        }

        return void 0;
    }

    public getSupportedNodeTemplates(node: ITreeNode) {
        return NodeInstance.getSupportedTypes(node, this._nodeTemplates);
    }

}
