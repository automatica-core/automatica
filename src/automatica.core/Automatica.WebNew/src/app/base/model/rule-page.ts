import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { RulePageType } from "./rule-page-type"
import { RuleInstance } from "./rule-instance"
import { NodeInstance2RulePage } from "./node-instance-2-rule-page"
import { Link } from "./link"
import { IPropertyModel } from "./interfaces";
import { INameModel } from "./INameModel";
import { IDescriptionModel } from "./IDescriptionModel";
import { PropertyInstance } from "./property-instance";
import { VirtualNamePropertyInstance, VirtualDescriptionPropertyInstance } from "./virtual-props";
import { EventEmitter } from "@angular/core"

@Model()
export class RulePage extends BaseModel implements IPropertyModel, INameModel, IDescriptionModel {

    onZoomIn = new EventEmitter<void>();
    onZoomToView = new EventEmitter<void>();
    onZoomOut = new EventEmitter<void>();

    public get DisplayName(): string {
        return this.Name;
    }
    public set DisplayName(v: string) {
        this.Name = v;
    }

    public get DisplayDescription(): string {
        return this.Description;
    }
    public set DisplayDescription(v: string) {
        this.Description = v;
    }

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    This2RulePageType: number;

    @JsonPropertyName("This2RulePageTypeNavigation")
    PageType: RulePageType;

    @JsonPropertyName("RuleInstance")
    RuleInstances: RuleInstance[] = [];

    @JsonPropertyName("NodeInstance2RulePage")
    NodeInstances: NodeInstance2RulePage[] = [];

    @JsonPropertyName("Link")
    Links: Link[] = [];

    @JsonProperty()
    IsSelected: boolean;

    Properties: PropertyInstance[] = [];

    constructor() {
        super();
        this.Links = [];
        this.RuleInstances = [];
        this.NodeInstances = [];

        this.This2RulePageType = 1;
    }

    public removeRuleInstance(objId: string) {
        this.RuleInstances = this.RuleInstances.filter(a => a.ObjId !== objId);
    }

    public removeNodeInstance(objId: string) {
        this.NodeInstances = this.NodeInstances.filter(a => a.ObjId !== objId);
    }

    public removeLink(link: Link): any {
        if (!link) {
            return;
        }
        this.Links = this.Links.filter(a => a.ObjId !== link.ObjId);
    }


    public getRuleInstance(objId: string): RuleInstance {
        return this.RuleInstances.find(a => a.ObjId === objId)[0];
    }

    public getNodeInstance(objId: string): NodeInstance2RulePage {
        return this.NodeInstances.find(a => a.ObjId === objId)[0];
    }

    private addVirtualProperties() {
        this.Properties.push(new VirtualNamePropertyInstance(this, false));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this, false));
    }

    afterFromJson() {
        this.addVirtualProperties();
    }

    protected createInstance(): BaseModel {
        return new RulePage();
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "RulePage";
    }
}
