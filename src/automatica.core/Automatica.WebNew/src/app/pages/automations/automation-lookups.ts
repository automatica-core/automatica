import { NodeInstance } from "src/app/base/model/node-instance";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import { InterfaceTypeEnum } from "src/app/base/model/interface-type";
import { Control } from "src/app/base/model/control";
import { AreaInstance } from "src/app/base/model/areas";
import { CategoryInstance } from "src/app/base/model/categories";
import { AutomationDto } from "src/app/base/model/automations/automation-dto";

export interface NamedOption {
    id: string;
    name: string;
}

export interface AreaOption extends NamedOption {
    /** "Ground floor › Hallway" */
    path: string;
}

export interface ControlOption extends NamedOption {
    type: string;
}

/** how a value of the datapoint is entered and shown */
export type ValueKind = "bool" | "number" | "string" | "windowState" | "unknown";

function key(id: string): string {
    return (id || "").toLowerCase();
}

/**
 * Names of the objects a rule refers to (datapoints, controls, areas, categories, other rules).
 */
export class AutomationLookups {

    controls: ControlOption[] = [];
    areas: AreaOption[] = [];
    categories: NamedOption[] = [];
    automations: NamedOption[] = [];

    private controlMap = new Map<string, ControlOption>();
    private areaMap = new Map<string, AreaOption>();
    private categoryMap = new Map<string, NamedOption>();
    private automationMap = new Map<string, NamedOption>();

    constructor(private nodeResolver: (id: string) => NodeInstance | undefined = () => void 0) {
    }

    setNodeResolver(resolver: (id: string) => NodeInstance | undefined) {
        this.nodeResolver = resolver;
    }

    setControls(controls: Control[]) {
        this.controls = (controls || [])
            .map(c => ({ id: key(c.Id), name: c.Name, type: (c.Type || "").toLowerCase() }))
            .sort((a, b) => (a.name || "").localeCompare(b.name || ""));
        this.controlMap = new Map(this.controls.map(c => [c.id, c]));
        this.switchControls = this.controls.filter(c => c.type === "switch" || c.type === "dimmer");
        this.blindControls = this.controls.filter(c => c.type === "blind");
        // the scene block of the operations plugin, the controls API names it by class name
        this.sceneControls = this.controls.filter(c => c.type === "scene");
    }

    /** scene blocks, for the trigger "scene is activated" */
    sceneControls: ControlOption[] = [];

    /** blinds, for the automatic shading */
    blindControls: ControlOption[] = [];

    /** switches and dimmers, they have an on/off state */
    switchControls: ControlOption[] = [];

    setAreas(areas: AreaInstance[]) {
        // the list may be flat or a tree, collect every area once
        const all = new Map<string, AreaInstance>();
        const visit = (a: AreaInstance) => {
            if (!a || all.has(key(a.ObjId))) {
                return;
            }
            all.set(key(a.ObjId), a);
            for (const c of a.InverseThis2ParentNavigation || []) {
                visit(c);
            }
        };
        for (const a of areas || []) {
            visit(a);
        }

        const pathOf = (a: AreaInstance, depth = 0): string => {
            const parent = a.This2Parent ? all.get(key(a.This2Parent)) : void 0;
            if (parent && depth < 10) {
                return pathOf(parent, depth + 1) + " › " + a.Name;
            }
            return a.Name;
        };

        this.areas = [...all.values()]
            .map(a => ({ id: key(a.ObjId), name: a.Name, path: pathOf(a) }))
            .sort((a, b) => a.path.localeCompare(b.path));
        this.areaMap = new Map(this.areas.map(a => [a.id, a]));
    }

    setCategories(categories: CategoryInstance[]) {
        this.categories = (categories || [])
            .map(c => ({ id: key(c.ObjId), name: c.DisplayName || c.Name }))
            .sort((a, b) => (a.name || "").localeCompare(b.name || ""));
        this.categoryMap = new Map(this.categories.map(c => [c.id, c]));
    }

    setAutomations(automations: AutomationDto[]) {
        this.automations = (automations || [])
            .map(a => ({ id: key(a.objId), name: a.name }))
            .sort((a, b) => (a.name || "").localeCompare(b.name || ""));
        this.automationMap = new Map(this.automations.map(a => [a.id, a]));
    }

    node(id: string): NodeInstance | undefined {
        if (!id) {
            return void 0;
        }
        return this.nodeResolver(id) ?? this.nodeResolver(key(id));
    }

    nodeName(id: string): string | undefined {
        const node = this.node(id);
        return node ? (node.DisplayName || node.Name) : void 0;
    }

    /** the two parents above the datapoint, "KNX › Lights" */
    nodePath(id: string): string {
        const parts: string[] = [];
        let cur: any = this.node(id)?.Parent;
        while (cur instanceof NodeInstance && cur.Parent instanceof NodeInstance && parts.length < 2) {
            parts.unshift(cur.DisplayName || cur.Name);
            cur = cur.Parent;
        }
        return parts.join(" › ");
    }

    valueKind(id: string): ValueKind {
        const node = this.node(id);
        if (!node?.NodeTemplate) {
            return "unknown";
        }
        switch (node.NodeTemplate.This2NodeDataType) {
            case NodeDataTypeEnum.Boolean:
                return "bool";
            case NodeDataTypeEnum.Integer:
            case NodeDataTypeEnum.Double:
                return "number";
            case NodeDataTypeEnum.WindowState:
                return "windowState";
            case NodeDataTypeEnum.NoAttribute:
                return "unknown";
            default:
                return "string";
        }
    }

    /** a datapoint (not a device or folder) */
    static isDatapoint(node: any): node is NodeInstance {
        return node instanceof NodeInstance && node.NodeTemplate?.ProvidesInterface2InterfaceType === InterfaceTypeEnum.Value;
    }

    control(id: string): ControlOption | undefined {
        return this.controlMap.get(key(id));
    }

    area(id: string): AreaOption | undefined {
        return this.areaMap.get(key(id));
    }

    categoryName(id: string): string | undefined {
        return this.categoryMap.get(key(id))?.name;
    }

    automationName(id: string): string | undefined {
        return this.automationMap.get(key(id))?.name;
    }

    /** operating modes (public holiday, vacation, own modes): id = mode key, name = display name */
    operatingModes: NamedOption[] = [];

    setOperatingModes(modes: NamedOption[]) {
        this.operatingModes = [...(modes || [])];
    }

    operatingModeName(mode: string | undefined): string | undefined {
        return this.operatingModes.find(a => a.id === mode)?.name;
    }
}
