import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { RuleInterfaceInstance } from "./rule-interface-instance"
import { RuleTemplate } from "./rule-template"
import { RulePage } from "./rule-page"
import { RuleInterfaceParameterDataType } from "./rule-interface-template"

import { IKey } from "./IKey";
import { VirtualPropertyInstance } from "./virtual-props/virtual-property-instance";
import { VirtualNamePropertyInstance } from "./virtual-props/virtual-name-property-instance";
import { VirtualDescriptionPropertyInstance } from "./virtual-props/virtual-description-property-instance";
import { IDescriptionModel } from "./IDescriptionModel";
import { INameModel } from "./INameModel";
import { Guid } from "../utils/Guid";
import { IPropertyModel } from "./interfaces/ipropertyModel";
import { RuleInterfaceParamProperty } from "./virtual-props/rule-instance/rule-interface-param-property";
import { ICategoryInstanceModel } from "./ICategoryInstanceModel";
import { IAreaInstanceModel } from "./IAreaInstanceModel";
import { VirtualUseInVisuPropertyInstance } from "./virtual-props/virtual-use-in-visu-property-instance";
import { VirtualAreaPropertyInstance } from "./virtual-props/virtual-area-property-instance";
import { VirtualCategoryPropertyInstance } from "./virtual-props/virtual-category-property-instance";
import { VirtualUserGroupPropertyInstance } from "./virtual-props/virtual-usergroup-property-instance";
import { AreaInstance } from "./areas"
import { CategoryInstance } from "./categories"
import { VisuObjectType } from "../visu/base-mobile-component"

function sortBySortOrder(a: RuleInterfaceInstance, b: RuleInterfaceInstance) {
    if (!a.Template) {
        return 0;
    }
    return a.Template.SortOrder - b.Template.SortOrder;
};


@Model()
export class RuleInstance extends BaseModel implements VisuObjectType, IKey, IDescriptionModel, INameModel, IPropertyModel, IAreaInstanceModel, ICategoryInstanceModel {
    
    public static KeyPrefix: string = "Rule";

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    This2RuleTemplate: string;

    _x: number;
    public get X() {
        return Math.round(this._x);
    }
    @JsonProperty()
    public set X(value: number) {
        this._x = value;
    }

    _y: number;
    public get Y() {
        return Math.round(this._y);
    }
    @JsonProperty()
    public set Y(value: number) {
        this._y = value;
    }

    @JsonPropertyName("This2RuleTemplateNavigation")
    RuleTemplate: RuleTemplate;

    public get RuleTemplateName(): string {
        if (!this.RuleTemplate) {
            return "";
        }
        return this.RuleTemplate.Name;
    }

    @JsonProperty()
    RulePage: RulePage;

    @JsonPropertyName("RuleInterfaceInstance")
    Interfaces: RuleInterfaceInstance[];

    @JsonProperty()
    UseInVisu: boolean;

    @JsonProperty()
    This2AreaInstance: string;

    @JsonProperty()
    This2CategoryInstance: string;

    @JsonProperty()
    This2AreaInstanceNavigation: AreaInstance;

    @JsonProperty()
    This2CategoryInstanceNavigation: CategoryInstance;

    @JsonProperty()
    This2UserGroup: string;

    Properties: VirtualPropertyInstance[] = [];

    public get DisplayName(): string {
        return this.Name;
    }

    private _DisplayDescription: string;
    public get DisplayDescription(): string {
        if (!this._DisplayDescription) {
            return this.Description;
        }
        return this._DisplayDescription;
    }
    public set DisplayDescription(v: string) {
        this._DisplayDescription = v;
    }


    public static fromRuleTemplate(template: RuleTemplate): RuleInstance {
        const instance = new RuleInstance();
        instance.This2RuleTemplate = template.ObjId;
        instance.RuleTemplate = template;
        instance.Name = template.Name;
        instance.Description = template.Description;

        instance.ObjId = Guid.MakeNew().ToString();

        instance.Interfaces = [];
        template.Interfaces.forEach(element => {
            const intre = RuleInterfaceInstance.fromTemplate(element, instance);
            intre.RuleInstance = instance;
            instance.Interfaces.push(intre);
        });

        instance.addVirtualProperties();
        return instance;
    }


    constructor() {
        super();
        this.X = 0;
        this.Y = 0;
    }

    get Location() {
        return this.X + " " + this.Y;
    }

    get key(): any {
        return RuleInstance.KeyPrefix + "-" + this.ObjId;
    }

    private addVirtualProperties() {
        this.Properties.push(new VirtualNamePropertyInstance(this));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this));


        this.Properties.push(new VirtualUseInVisuPropertyInstance(this));
        this.Properties.push(new VirtualAreaPropertyInstance(this));
        this.Properties.push(new VirtualCategoryPropertyInstance(this));

        this.Properties.push(new VirtualUserGroupPropertyInstance(this));


        for (const x of this.Interfaces) {
            if (x.Template.ParameterDataType === RuleInterfaceParameterDataType.NoParameter) {
                continue;
            }
            this.Properties.push(new RuleInterfaceParamProperty(x));
        }
    }

    protected afterFromJson() {
        this.Interfaces.sort(sortBySortOrder);

        this.addVirtualProperties();
    }


    set Location(value: string) {
        const split = value.split(" ");

        this.X = parseFloat(split[0]);
        this.Y = parseFloat(split[1]);
    }


    get category(): string {
        return RuleInstance.KeyPrefix;
    }

    get Inputs(): RuleInterfaceInstance[] {
        const interfaces = new Array<RuleInterfaceInstance>();
        this.Interfaces.forEach(element => {
            if (element.Template.InterfaceDirection.Key === "I") {
                interfaces.push(element);
            }
        });

        return [...interfaces, ...this.LinkableParameters];
    }

    get Outputs(): RuleInterfaceInstance[] {
        const interfaces = new Array<RuleInterfaceInstance>();
        this.Interfaces.forEach(element => {
            if (element.Template.InterfaceDirection.Key === "O") {
                interfaces.push(element);
            }
        });

        return interfaces;
    }

    get LinkableParameters(): RuleInterfaceInstance[] {
        const interfaces = new Array<RuleInterfaceInstance>();
        this.Interfaces.forEach(element => {
            if (element.Template.InterfaceDirection.Key === "P" && element.Template.IsLinkableParameter) {
                interfaces.push(element);
            }
        });

        return interfaces;
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
    public typeInfo(): string {
        return "RuleInstance";
    }

    public validate() {
        return true;
    }
}
