import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { NodeInstance } from "./node-instance"
import { RulePage } from "./rule-page"

import { Guid } from "../utils/Guid";
import { IPropertyModel } from "./interfaces/ipropertyModel";
import { PropertyInstance } from "./property-instance";

export class NodeInterfaceInstance {
    ObjId: string;
    PortId: any;
    Name: string;
    NodeInstance2RulePageId: string;

    PortValue: any;
}

@Model()

export class NodeInstance2RulePage extends BaseModel implements IPropertyModel {
    public get Properties(): PropertyInstance[] {
        if (!this.NodeInstance) {
            return [];
        }
        return this.NodeInstance.Properties;
    }

    public set Properties(value) {

    }

    public static KeyPrefix: string = "Attribute";

    @JsonProperty()
    ObjId: string;

    public itemMoved: boolean = false;

    _x: number;
    public get X() {
        return Math.round(this._x);
    }
    @JsonProperty()
    public set X(value: number) {
        this._x = value;
        this.itemMoved = true;
    }

    _y: number;
    public get Y() {
        return Math.round(this._y);
    }
    @JsonProperty()
    public set Y(value: number) {
        this._y = value;
        this.itemMoved = true;
    }

    public get FullName() {
        return this.NodeInstance.FullName;
    }

    private _inverted: boolean;
    
    @JsonProperty()
    public get Inverted(): boolean {
        return this._inverted;
    }
    public set Inverted(v: boolean) {
        this._inverted = v;
        this.notifyChange("Inverted");
    }

    @JsonPropertyName("This2NodeInstanceNavigation")
    NodeInstance: NodeInstance;
    @JsonProperty()
    RulePage: RulePage;

    @JsonProperty()
    This2NodeInstance: string;

    @JsonProperty()
    This2RulePage: string;

    Inputs: NodeInterfaceInstance[] = [];
    Outputs: NodeInterfaceInstance[] = [];

    public get Id() {
        return this.ObjId;
    }

    public get ParentId() {
        return this.This2NodeInstance;
    }
    public get DisplayName() {
        return this.RulePage.Name;
    }

    public setSaved(): void {
        this.itemMoved = false;
    }

    public static createFromNodeInstance(nodeInstance: NodeInstance, rulePage: RulePage) {
        const nd = new NodeInstance2RulePage();
        nd.NodeInstance = nodeInstance;
        nd.This2NodeInstance = nodeInstance.ObjId;
        nd.This2RulePage = rulePage.ObjId;
        nd.ObjId = Guid.MakeNew().ToString();
        nd.afterFromJson();
        return nd;
    }

    constructor() {
        super();
    }

    afterFromJson() {
        if (this.NodeInstance.NodeTemplate.IsReadable) {
            const intf = new NodeInterfaceInstance();
            intf.Name = this.ObjId + "O";
            intf.PortId = this.ObjId + "O";
            intf.ObjId = this.This2NodeInstance;
            intf.NodeInstance2RulePageId = this.ObjId;

            this.Outputs.push(intf);
        }
        if (this.NodeInstance.NodeTemplate.IsWriteable) {
            const intf = new NodeInterfaceInstance();
            intf.Name = this.ObjId + "I";
            intf.PortId = this.ObjId + "I";
            intf.ObjId = this.This2NodeInstance;
            intf.NodeInstance2RulePageId = this.ObjId;

            this.Inputs.push(intf);
        }
    }


    get Name(): string {
        if (!this.NodeInstance) {
            return "unknown";
        }
        return this.NodeInstance.Name;
    }

    get Location() {
        return this.X + " " + this.Y;
    }

    set Location(value: string) {
        const split = value.split(" ");

        this.X = parseFloat(split[0]);
        this.Y = parseFloat(split[1]);
    }

    get category(): string {
        return NodeInstance2RulePage.KeyPrefix;
    }

    get key(): any {
        return NodeInstance2RulePage.KeyPrefix + "-" + this.ObjId;
    }


    get FromMaxLinks(): number {
        return 1;
    }

    get ToMaxLinks(): number {
        return 1;
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "NodeInstance2RulePage";
    }
}
