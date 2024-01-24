import { Injectable } from "@angular/core";
import { DesignTimeDataService } from "./design-time-data.service";
import { ConfigService } from "./config.service";
import { NodeInstance, ValueSource } from "../base/model/node-instance";
import { NodeTemplate } from "../base/model/node-template";
import { VirtualSettingsPropertyInstance } from "../base/model/virtual-props/settings/settings-property-instance";
import { SettingsService } from "./settings.service";
import { Setting } from "../base/model/setting";
import { ITreeNode } from "../base/model/ITreeNode";
import { DataHubService } from "../base/communication/hubs/data-hub.service";
import { NodeTemplateService } from "./node-template.service";
import { DataService } from "./data.service";
import { NodeInstance2RulePage } from "../base/model/node-instance-2-rule-page";

type NodeInstanceOrLogicNodeInstance = NodeInstance | NodeInstance2RulePage;

@Injectable()
export class NodeInstanceService {


    private _nodeTemplates: NodeTemplate[];
    private _nodeInstanceMap: Map<string, NodeInstance>;
    private _nodeInstanceList: NodeInstanceOrLogicNodeInstance[];
    private _settings: Setting[];

    private _logicNodeInstance: Map<string, NodeInstance2RulePage>;


    private _rootNode: NodeInstance;

    public get rootNode(): NodeInstance {
        return this._rootNode;
    }
    public set rootNode(v: NodeInstance) {
        this._rootNode = v;
    }

    public get nodeInstanceList(): NodeInstanceOrLogicNodeInstance[] {
        return this._nodeInstanceList;
    }

    constructor(private designTimeDataService: DesignTimeDataService,
        private configService: ConfigService,
        private settingsService: SettingsService,
        private dataHubService: DataHubService,
        private nodeTemplateService: NodeTemplateService,
        private dataService: DataService) {

    }

    public async load() {
        this._settings = await this.settingsService.getSettings();
        await this.loadConfig();
        await this.dataHubService.subscribeForAll();

        const currentValues = await this.dataService.getCurrentNodeValues();
        const that = this;

        Object.keys(currentValues).forEach(function (key) {
            that.setNodeInstanceValue(key, currentValues[key]);
        });
    }

    public getNodeInstance(id: string): NodeInstance {
        return this._nodeInstanceMap.get(id);
    }
    getLogicNodeInstance(id: any) {
        return this._logicNodeInstance.get(id);
    }

    public hasNodeInstance(id: string) {
        if (!this._nodeInstanceMap) {
            return false;
        }
        return this._nodeInstanceMap.has(id);
    }

    public setNodeInstanceValue(id: string, value: any) {
        if (this.hasNodeInstance(id)) {
            const nodeInstance = this._nodeInstanceMap.get(id);
            nodeInstance.Value = value.value;
            nodeInstance.ValueTimestamp = value.timestamp;
            nodeInstance.ValueSource = value.valueSource;

            if (value.valueSource == ValueSource.Read) {
                nodeInstance.ReadValue = value.value;
            }
            else if (value.valueSource == ValueSource.Write) {
                nodeInstance.WriteValue = value.value;
            }
        }
    }

    private async loadConfig() {
        const instances = await this.configService.getNodeInstances();
        const logicNodeInstances = await this.configService.getLogicNodeInstances();

        this.convertConfig(instances);
        this.convertLogicNodeInstances(logicNodeInstances);

    }
    convertLogicNodeInstances(logicNodeInstances: NodeInstance2RulePage[]) {
        this._logicNodeInstance = new Map<string, NodeInstance2RulePage>();

        for (const node of logicNodeInstances) {
            this._logicNodeInstance.set(node.ObjId, node);

            this._nodeInstanceList.push(node);
        }
    }

    public addLogicNodeInstance(node: NodeInstance2RulePage) {
        this._logicNodeInstance.set(node.Id, node);
        this._nodeInstanceList.push(node);
    }

    updateNodeInstance(nodeInstance: NodeInstance) {
        this.addNodeInstance(nodeInstance);

        if (nodeInstance.Children) {
            for (const child of nodeInstance.Children) {
                this.updateNodeInstance(child);
            }
        }
    }

    public convertConfig(instances: NodeInstance[]) {
        this._nodeInstanceMap = new Map<string, NodeInstance>();

        this._nodeInstanceList = [];

        const tmpConfig: NodeInstanceOrLogicNodeInstance[] = [];

        for (const node of instances) {
            this.addNodeInstancesRec(node, tmpConfig);
        }
        tmpConfig.sort((a, b) => (a.Name.localeCompare(b.Name) - (b.Name.localeCompare(a.Name))));

        this._nodeInstanceList = tmpConfig;

        const rootNode = this._nodeInstanceList.filter(a => a instanceof NodeInstance && !a.This2ParentNodeInstance)[0];
        const rootNodeSettings: VirtualSettingsPropertyInstance[] = [];

        for (const x of this._settings) {
            rootNodeSettings.push(new VirtualSettingsPropertyInstance(x));
        }

        rootNode.Properties = [...rootNode.Properties, ...rootNodeSettings];
        if (rootNode instanceof NodeInstance)
            this.rootNode = rootNode;

        return this._nodeInstanceList;
    }

    public addNodeInstance(node: NodeInstance) {
        this._nodeInstanceMap.set(node.Id, node);
        this._nodeInstanceList.push(node);
    }

    removeLogicNodeInstance(item: NodeInstance2RulePage) {
        this._nodeInstanceList = this._nodeInstanceList.filter(a => a.Id !== item.Id);
        if(this._logicNodeInstance.has(item.Id))
            this._logicNodeInstance.delete(item.Id);
    }

    public removeItem(node: ITreeNode) {
        this._nodeInstanceList = this._nodeInstanceList.filter(a => a.Id !== node.Id);
        this._nodeInstanceMap.delete(node.Id);
    }

    public addNodeInstancesRec(node: NodeInstance, tmpConfig: NodeInstanceOrLogicNodeInstance[]) {
        this._nodeInstanceMap.set(node.Id, node);
        tmpConfig.push(node);

        for (const x of node.Children) {
            this.addNodeInstancesRec(x, tmpConfig);
        }
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

    public async getSupportedNodeTemplates(node: NodeInstance) {

        if (node.NodeTemplate.ProvidesInterface2InterfaceType === NodeTemplate.ValueInterfaceId()) {
            return [];
        }

        return await this.nodeTemplateService.getSupportedTemplates(node, node.NodeTemplate.ProvidesInterface2InterfaceType);
    }

    public async saveSettings() {
        const settings = [];
        for (const x of this.rootNode.Properties) {
            if (x instanceof VirtualSettingsPropertyInstance) {
                settings.push(x.setting);
            }
        }
        await this.settingsService.saveSettings(settings);
    }



}